using System.Diagnostics;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Modules.Transactions.Infrastructure;

public sealed class TransactionsModuleHealthCheck : IModuleHealthCheck
{
    public string ModuleName => "Transactions";

    public Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        return Task.FromResult(new ModuleHealthResult(
            ModuleHealthStatus.Healthy,
            stopwatch.ElapsedMilliseconds));
    }
}
