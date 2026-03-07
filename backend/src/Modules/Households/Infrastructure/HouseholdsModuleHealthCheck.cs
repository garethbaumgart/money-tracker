using System.Diagnostics;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class HouseholdsModuleHealthCheck : IModuleHealthCheck
{
    public string ModuleName => "Households";

    public Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        return Task.FromResult(new ModuleHealthResult(
            ModuleHealthStatus.Healthy,
            stopwatch.ElapsedMilliseconds));
    }
}
