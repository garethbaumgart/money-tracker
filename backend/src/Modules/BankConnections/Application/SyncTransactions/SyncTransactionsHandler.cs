using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Transactions;

namespace MoneyTracker.Modules.BankConnections.Application.SyncTransactions;

public sealed class SyncTransactionsHandler(
    IBankConnectionRepository connectionRepository,
    IBankProviderAdapter providerAdapter,
    ITransactionSyncRepository transactionSyncRepository,
    TimeProvider timeProvider,
    ILogger<SyncTransactionsHandler> logger)
{
    public async Task<SyncTransactionsResult> HandleAsync(
        SyncTransactionsCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BankConnection> connections;

        if (command.HouseholdId.HasValue)
        {
            connections = await connectionRepository.GetByHouseholdAsync(
                command.HouseholdId.Value,
                cancellationToken);

            connections = connections
                .Where(c => c.Status == BankConnectionStatus.Active)
                .ToArray();
        }
        else
        {
            connections = await connectionRepository.GetActiveConnectionsAsync(cancellationToken);
        }

        var totalSynced = 0;
        var totalSkipped = 0;
        var totalFailed = 0;

        foreach (var connection in connections)
        {
            try
            {
                var (synced, skipped) = await SyncConnectionAsync(connection, cancellationToken);
                totalSynced += synced;
                totalSkipped += skipped;

                var nowUtc = timeProvider.GetUtcNow();
                connection.RecordSyncSuccess(nowUtc);
                await connectionRepository.UpdateAsync(connection, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                totalFailed += 1;
                logger.LogError(
                    ex,
                    "Sync failed for connection={ConnectionId} household={HouseholdId}",
                    connection.Id.Value,
                    connection.HouseholdId);

                var nowUtc = timeProvider.GetUtcNow();
                connection.RecordSyncFailure(nowUtc);
                await connectionRepository.UpdateAsync(connection, cancellationToken);
            }
        }

        return SyncTransactionsResult.Success(totalSynced, totalSkipped, totalFailed);
    }

    private async Task<(int Synced, int Skipped)> SyncConnectionAsync(
        BankConnection connection,
        CancellationToken cancellationToken)
    {
        var sinceUtc = connection.SyncState.LastSyncCursorUtc
                       ?? connection.CreatedAtUtc;

        var providerResult = await providerAdapter.GetTransactionsAsync(
            connection.ExternalConnectionId,
            sinceUtc,
            cancellationToken);

        if (!providerResult.IsSuccess || providerResult.Transactions is null)
        {
            throw new InvalidOperationException(
                providerResult.ErrorMessage ?? "Provider returned an error fetching transactions.");
        }

        var synced = 0;
        var skipped = 0;
        var newTransactions = new List<SyncedTransaction>();

        foreach (var providerTxn in providerResult.Transactions)
        {
            var exists = await transactionSyncRepository.ExistsByExternalIdAsync(
                connection.Id.Value,
                providerTxn.ExternalTransactionId,
                cancellationToken);

            if (exists)
            {
                skipped += 1;
                continue;
            }

            var nowUtc = timeProvider.GetUtcNow();
            var transaction = new SyncedTransaction(
                connection.HouseholdId,
                connection.Id.Value,
                providerTxn.ExternalTransactionId,
                providerTxn.Amount,
                providerTxn.PostedAtUtc,
                providerTxn.Description,
                nowUtc);

            newTransactions.Add(transaction);
            synced += 1;
        }

        if (newTransactions.Count > 0)
        {
            await transactionSyncRepository.AddSyncedTransactionsAsync(newTransactions, cancellationToken);
        }

        return (synced, skipped);
    }
}
