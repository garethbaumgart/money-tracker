using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class TransactionSyncWorker(
    SyncTransactionsHandler handler,
    ILogger<TransactionSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var result = await handler.HandleAsync(
                    new SyncTransactionsCommand(HouseholdId: null),
                    stoppingToken);
                logger.LogInformation(
                    "Transaction sync completed: synced={Synced} skipped={Skipped} failed={Failed}",
                    result.SyncedCount,
                    result.SkippedCount,
                    result.FailedConnections);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Transaction sync worker failed.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
