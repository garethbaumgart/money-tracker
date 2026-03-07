using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Transactions;

namespace MoneyTracker.Modules.BankConnections.Application.SyncTransactions;

public sealed class SyncTransactionsHandler(
    IBankConnectionRepository connectionRepository,
    IBankProviderAdapter providerAdapter,
    ITransactionSyncRepository transactionSyncRepository,
    ISyncEventRepository syncEventRepository,
    TimeProvider timeProvider,
    ILogger<SyncTransactionsHandler> logger)
{
    private const string DefaultRegion = "AU";

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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var (synced, skipped) = await SyncConnectionAsync(connection, cancellationToken);
                totalSynced += synced;
                totalSkipped += skipped;
                stopwatch.Stop();

                var nowUtc = timeProvider.GetUtcNow();
                connection.RecordSyncSuccess(nowUtc);
                await connectionRepository.UpdateAsync(connection, cancellationToken);

                await RecordSyncEventAsync(
                    connection,
                    EventOutcome.Success,
                    stopwatch.ElapsedMilliseconds,
                    synced + skipped,
                    errorCategory: null,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                totalFailed += 1;
                logger.LogError(
                    ex,
                    "Sync failed for connection={ConnectionId} household={HouseholdId}",
                    connection.Id.Value,
                    connection.HouseholdId);

                var nowUtc = timeProvider.GetUtcNow();
                connection.RecordSyncFailure(nowUtc);
                await connectionRepository.UpdateAsync(connection, cancellationToken);

                await RecordSyncEventAsync(
                    connection,
                    EventOutcome.Failed,
                    stopwatch.ElapsedMilliseconds,
                    transactionCount: 0,
                    errorCategory: ex.GetType().Name,
                    cancellationToken);
            }
        }

        return SyncTransactionsResult.Success(totalSynced, totalSkipped, totalFailed);
    }

    private async Task RecordSyncEventAsync(
        BankConnection connection,
        EventOutcome outcome,
        long durationMs,
        int transactionCount,
        string? errorCategory,
        CancellationToken cancellationToken)
    {
        try
        {
            var institution = connection.InstitutionName ?? "Unknown";
            var region = DefaultRegion;

            var syncEvent = SyncEvent.Create(
                connection.Id.Value,
                institution,
                region,
                outcome,
                durationMs,
                transactionCount,
                errorCategory,
                timeProvider.GetUtcNow());

            await syncEventRepository.AddAsync(syncEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to record sync event for connection={ConnectionId}",
                connection.Id.Value);
        }
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
