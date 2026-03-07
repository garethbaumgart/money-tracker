using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;

public sealed class ProcessWebhookHandler(
    IWebhookSignatureValidator signatureValidator,
    SyncTransactionsHandler syncHandler,
    IBankConnectionRepository connectionRepository,
    IConsentNotificationSender consentNotificationSender,
    TimeProvider timeProvider,
    ILogger<ProcessWebhookHandler> logger)
{
    public async Task<ProcessWebhookResult> HandleAsync(
        ProcessWebhookCommand command,
        CancellationToken cancellationToken)
    {
        if (!signatureValidator.Validate(command.Signature, command.RawBody))
        {
            logger.LogWarning("Webhook received with invalid signature.");
            return ProcessWebhookResult.InvalidSignature();
        }

        logger.LogInformation(
            "Webhook received eventType={EventType} connectionId={ConnectionId}",
            command.EventType,
            command.ConnectionId);

        if (string.Equals(command.EventType, "consent.revoked", StringComparison.OrdinalIgnoreCase))
        {
            await HandleConsentRevocationAsync(command.ConnectionId, cancellationToken);
            return ProcessWebhookResult.Accepted();
        }

        // Trigger sync — idempotent because SyncTransactionsHandler deduplicates
        // by ExternalTransactionId. Replayed webhooks will not create duplicates.
        await syncHandler.HandleAsync(
            new SyncTransactionsCommand(HouseholdId: null),
            cancellationToken);

        return ProcessWebhookResult.Accepted();
    }

    private async Task HandleConsentRevocationAsync(
        string? externalConnectionId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalConnectionId))
        {
            logger.LogWarning("Consent revocation webhook received without connection ID.");
            return;
        }

        var connection = await connectionRepository.GetByExternalConnectionIdAsync(
            externalConnectionId, cancellationToken);

        if (connection is null)
        {
            logger.LogWarning(
                "Consent revocation webhook for unknown connection={ConnectionId}",
                externalConnectionId);
            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        connection.MarkConsentRevoked(nowUtc);
        await connectionRepository.UpdateAsync(connection, cancellationToken);
        await consentNotificationSender.SendConsentRevokedAsync(connection, cancellationToken);

        logger.LogInformation(
            "Consent revoked for connection={ConnectionId} household={HouseholdId}",
            connection.Id.Value,
            connection.HouseholdId);
    }
}
