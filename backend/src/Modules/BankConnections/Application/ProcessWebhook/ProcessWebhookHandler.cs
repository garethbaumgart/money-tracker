using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;

public sealed class ProcessWebhookHandler(
    IWebhookSignatureValidator signatureValidator,
    SyncTransactionsHandler syncHandler,
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

        // Trigger sync — idempotent because SyncTransactionsHandler deduplicates
        // by ExternalTransactionId. Replayed webhooks will not create duplicates.
        await syncHandler.HandleAsync(
            new SyncTransactionsCommand(HouseholdId: null),
            cancellationToken);

        return ProcessWebhookResult.Accepted();
    }
}
