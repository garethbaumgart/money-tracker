using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;

public sealed class ProcessRevenueCatWebhookHandler(
    IRevenueCatWebhookSignatureValidator signatureValidator,
    ISubscriptionRepository subscriptionRepository,
    TimeProvider timeProvider,
    ILogger<ProcessRevenueCatWebhookHandler> logger)
{
    public async Task<ProcessRevenueCatWebhookResult> HandleAsync(
        ProcessRevenueCatWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (!signatureValidator.Validate(command.Signature, command.RawBody))
        {
            logger.LogWarning("RevenueCat webhook received with invalid signature.");
            return ProcessRevenueCatWebhookResult.InvalidSignature();
        }

        if (string.IsNullOrWhiteSpace(command.EventType))
        {
            logger.LogWarning("RevenueCat webhook received without event type.");
            return ProcessRevenueCatWebhookResult.InvalidPayload("Event type is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AppUserId))
        {
            logger.LogWarning("RevenueCat webhook received without app_user_id.");
            return ProcessRevenueCatWebhookResult.InvalidPayload("App user ID is required.");
        }

        logger.LogInformation(
            "RevenueCat webhook received eventType={EventType} appUserId={AppUserId} eventId={EventId}",
            command.EventType,
            command.AppUserId,
            command.EventId);

        var nowUtc = timeProvider.GetUtcNow();

        // Look up existing subscription by app user ID
        var subscription = await subscriptionRepository.GetByRevenueCatAppUserIdAsync(
            command.AppUserId, cancellationToken);

        // Check idempotency — if the event was already processed, acknowledge silently
        if (subscription is not null
            && !string.IsNullOrWhiteSpace(command.EventId)
            && subscription.HasProcessedEvent(command.EventId))
        {
            logger.LogInformation(
                "Duplicate RevenueCat event ignored eventId={EventId} appUserId={AppUserId}",
                command.EventId,
                command.AppUserId);
            return ProcessRevenueCatWebhookResult.Accepted();
        }

        var eventType = command.EventType.ToUpperInvariant();

        switch (eventType)
        {
            case "INITIAL_PURCHASE":
                await HandleInitialPurchaseAsync(command, subscription, nowUtc, cancellationToken);
                break;

            case "RENEWAL":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "RENEWAL event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                subscription.Renew(
                    command.PeriodStartUtc ?? nowUtc,
                    command.PeriodEndUtc ?? nowUtc.AddDays(30),
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            case "CANCELLATION":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "CANCELLATION event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                subscription.Cancel(
                    command.CancelledAtUtc ?? nowUtc,
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            case "EXPIRATION":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "EXPIRATION event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                subscription.Expire(
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            case "BILLING_ISSUE":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "BILLING_ISSUE event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                subscription.MarkBillingIssue(
                    nowUtc,
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            case "PRODUCT_CHANGE":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "PRODUCT_CHANGE event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                if (string.IsNullOrWhiteSpace(command.ProductId))
                {
                    logger.LogWarning(
                        "PRODUCT_CHANGE event without product_id appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.InvalidPayload("Product ID is required for PRODUCT_CHANGE.");
                }
                subscription.ChangeProduct(
                    command.ProductId,
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            case "TRANSFER":
                if (subscription is null)
                {
                    logger.LogWarning(
                        "TRANSFER event for unknown subscription appUserId={AppUserId}",
                        command.AppUserId);
                    return ProcessRevenueCatWebhookResult.Accepted();
                }
                subscription.Revoke(
                    command.EventId ?? Guid.NewGuid().ToString(),
                    nowUtc);
                await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
                break;

            default:
                logger.LogWarning(
                    "Unknown RevenueCat event type={EventType} appUserId={AppUserId}",
                    command.EventType,
                    command.AppUserId);
                break;
        }

        logger.LogInformation(
            "RevenueCat webhook processed eventType={EventType} appUserId={AppUserId} eventId={EventId}",
            command.EventType,
            command.AppUserId,
            command.EventId);

        return ProcessRevenueCatWebhookResult.Accepted();
    }

    private async Task HandleInitialPurchaseAsync(
        ProcessRevenueCatWebhookCommand command,
        Subscription? subscription,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (subscription is null)
        {
            // No existing subscription — create a new one in None status, then activate
            // The app_user_id maps to household ID (per architecture decision)
            if (!Guid.TryParse(command.AppUserId, out var householdId))
            {
                // If app_user_id is not a GUID, use a deterministic GUID from the string
                householdId = GenerateDeterministicGuid(command.AppUserId!);
            }

            subscription = Subscription.CreateForWebhook(
                householdId,
                command.AppUserId!,
                command.ProductId ?? "unknown",
                nowUtc);

            subscription.Activate(
                command.PeriodStartUtc ?? nowUtc,
                command.PeriodEndUtc ?? nowUtc.AddDays(30),
                command.OriginalPurchaseDateUtc ?? nowUtc,
                command.EventId ?? Guid.NewGuid().ToString(),
                nowUtc);

            await subscriptionRepository.AddAsync(subscription, cancellationToken);
        }
        else
        {
            // Existing subscription (e.g., was in Trial) — transition to Active
            subscription.Activate(
                command.PeriodStartUtc ?? nowUtc,
                command.PeriodEndUtc ?? nowUtc.AddDays(30),
                command.OriginalPurchaseDateUtc ?? nowUtc,
                command.EventId ?? Guid.NewGuid().ToString(),
                nowUtc);

            await subscriptionRepository.UpdateAsync(subscription, cancellationToken);
        }
    }

    private static Guid GenerateDeterministicGuid(string input)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
