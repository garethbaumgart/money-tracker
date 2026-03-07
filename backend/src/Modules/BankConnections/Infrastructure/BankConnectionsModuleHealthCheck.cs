using System.Diagnostics;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class BankConnectionsModuleHealthCheck(
    ISyncEventRepository syncEventRepository,
    TimeProvider timeProvider) : IModuleHealthCheck
{
    public string ModuleName => "BankConnections";

    public async Task<ModuleHealthResult> CheckAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var since = timeProvider.GetUtcNow().AddHours(-24);
            var events = await syncEventRepository.GetByPeriodAsync(since, ct);
            stopwatch.Stop();

            var total = events.Count;
            var successful = events.Count(e => e.Outcome == EventOutcome.Success);
            var successRate = total > 0 ? (double)successful / total * 100 : 100;

            var status = successRate switch
            {
                < 50 => ModuleHealthStatus.Unhealthy,
                < 90 => ModuleHealthStatus.Degraded,
                _ => ModuleHealthStatus.Healthy
            };

            return new ModuleHealthResult(
                status,
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, object>
                {
                    ["recentSyncTotal"] = total,
                    ["recentSyncSuccessful"] = successful,
                    ["syncSuccessRate"] = Math.Round(successRate, 1)
                });
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new ModuleHealthResult(
                ModuleHealthStatus.Unhealthy,
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, object>
                {
                    ["error"] = "Failed to query sync event repository"
                });
        }
    }
}
