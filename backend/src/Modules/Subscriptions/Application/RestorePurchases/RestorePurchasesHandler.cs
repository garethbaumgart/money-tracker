using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.RestorePurchases;

public sealed class RestorePurchasesHandler(
    ISubscriptionRepository repository,
    IRevenueCatClient revenueCatClient,
    IHouseholdAccessService householdAccess,
    ISubscriptionEntitlementService entitlementService,
    ILogger<RestorePurchasesHandler> logger)
{
    public async Task<RestorePurchasesResult> HandleAsync(
        RestorePurchasesCommand command,
        CancellationToken cancellationToken)
    {
        // AC-8: Verify household membership
        var accessResult = await householdAccess.CheckMemberAsync(
            command.HouseholdId,
            command.UserId,
            cancellationToken);

        if (!accessResult.HouseholdExists)
        {
            return RestorePurchasesResult.HouseholdNotFound();
        }

        if (!accessResult.IsMember)
        {
            return RestorePurchasesResult.AccessDenied();
        }

        // AC-5: Call RevenueCat REST API
        SubscriberInfo? subscriberInfo;
        try
        {
            subscriberInfo = await revenueCatClient.GetSubscriberAsync(
                command.RevenueCatAppUserId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to fetch subscriber info from RevenueCat for {AppUserId}.",
                command.RevenueCatAppUserId);
            return RestorePurchasesResult.ProviderError(
                "Failed to communicate with the payment provider.");
        }

        // Get or create local subscription record
        var subscription = await repository.GetByHouseholdIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (subscription is null)
        {
            // No local subscription exists; create one
            if (subscriberInfo is null)
            {
                // No subscription in provider either — return Free state
                var freeEntitlements = EntitlementSet.ForTier(SubscriptionTier.Free);
                return RestorePurchasesResult.Success(
                    SubscriptionStatus.None,
                    SubscriptionTier.Free,
                    freeEntitlements.FeatureKeys.Select(fk => fk.ToString()).ToArray(),
                    null);
            }

            subscription = Subscription.CreateForWebhook(
                command.HouseholdId,
                command.RevenueCatAppUserId,
                subscriberInfo.ProductId,
                DateTimeOffset.UtcNow);
            await repository.AddAsync(subscription, cancellationToken);
        }

        // AC-6: Update local subscription to match RevenueCat authoritative state
        if (subscriberInfo is not null)
        {
            subscription.RestoreFromProvider(
                subscriberInfo.Status,
                subscriberInfo.ProductId,
                subscriberInfo.PeriodStartUtc,
                subscriberInfo.PeriodEndUtc);
            await repository.UpdateAsync(subscription, cancellationToken);

            logger.LogInformation(
                "Subscription restored for household {HouseholdId}: status={Status}, product={ProductId}.",
                command.HouseholdId,
                subscriberInfo.Status,
                subscriberInfo.ProductId);
        }

        // Return current entitlement state
        var entitlement = await entitlementService.GetEntitlementsAsync(
            command.HouseholdId,
            cancellationToken);

        return RestorePurchasesResult.Success(
            subscription.Status,
            entitlement.Tier,
            entitlement.FeatureKeys.Select(fk => fk.ToString()).ToArray(),
            subscription.CurrentPeriodEndUtc);
    }
}
