using System.Diagnostics;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Modules.Notifications.Infrastructure;

public sealed class NotificationsModuleHealthCheck : IModuleHealthCheck
{
    public string ModuleName => "Notifications";

    public Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        return Task.FromResult(new ModuleHealthResult(
            ModuleHealthStatus.Healthy,
            stopwatch.ElapsedMilliseconds));
    }
}
