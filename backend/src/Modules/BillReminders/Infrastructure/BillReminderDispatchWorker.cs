using Microsoft.Extensions.Hosting;
using MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;

namespace MoneyTracker.Modules.BillReminders.Infrastructure;

public sealed class BillReminderDispatchWorker(
    DispatchDueRemindersHandler handler) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await handler.HandleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
