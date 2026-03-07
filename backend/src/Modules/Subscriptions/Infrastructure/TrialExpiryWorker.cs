using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Subscriptions.Application.ExpireTrial;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

public sealed class TrialExpiryWorker(
    ExpireTrialHandler handler,
    TimeProvider timeProvider,
    ILogger<TrialExpiryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var nowUtc = timeProvider.GetUtcNow();
                var result = await handler.HandleAsync(
                    new ExpireTrialCommand(nowUtc),
                    stoppingToken);
                logger.LogInformation(
                    "Trial expiry check completed: expired={ExpiredCount}",
                    result.ExpiredCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Trial expiry worker failed.");
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
