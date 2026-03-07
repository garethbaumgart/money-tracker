using System.Diagnostics;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Api.Health;

public sealed class AuthModuleHealthCheck : IModuleHealthCheck
{
    public string ModuleName => "Auth";

    public Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        stopwatch.Stop();

        return Task.FromResult(new ModuleHealthResult(
            ModuleHealthStatus.Healthy,
            stopwatch.ElapsedMilliseconds));
    }
}
