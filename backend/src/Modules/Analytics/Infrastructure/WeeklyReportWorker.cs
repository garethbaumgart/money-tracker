using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Analytics.Application.GenerateWeeklyReport;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class WeeklyReportWorker(
    GenerateWeeklyReportHandler handler,
    ILogger<WeeklyReportWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var result = await handler.HandleAsync(
                    new GenerateWeeklyReportCommand(),
                    stoppingToken);
                logger.LogInformation(
                    "Weekly report generated: reportId={ReportId} success={Success}",
                    result.ReportId,
                    result.IsSuccess);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Weekly report worker failed.");
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
