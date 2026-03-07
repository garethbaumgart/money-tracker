using System.Diagnostics;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Modules.Budgets.Infrastructure;

public sealed class BudgetsModuleHealthCheck : IModuleHealthCheck
{
    public string ModuleName => "Budgets";

    public Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        return Task.FromResult(new ModuleHealthResult(
            ModuleHealthStatus.Healthy,
            stopwatch.ElapsedMilliseconds));
    }
}
