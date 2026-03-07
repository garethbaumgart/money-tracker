using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class ConsentExpiryCheckWorker(
    CheckConsentExpiryHandler handler,
    ILogger<ConsentExpiryCheckWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var result = await handler.HandleAsync(
                    new CheckConsentExpiryCommand(),
                    stoppingToken);
                logger.LogInformation(
                    "Consent expiry check completed: expiringSoon={ExpiringSoon} expired={Expired} notifications={Notifications}",
                    result.ExpiringSoonCount,
                    result.ExpiredCount,
                    result.NotificationsCreated);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Consent expiry check worker failed.");
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
