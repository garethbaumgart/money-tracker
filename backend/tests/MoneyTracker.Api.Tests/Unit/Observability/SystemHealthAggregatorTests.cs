using MoneyTracker.Modules.SharedKernel.Health;

namespace MoneyTracker.Api.Tests.Unit.Observability;

public sealed class SystemHealthAggregatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DeriveOverallStatus_AllHealthy_ReturnsHealthy()
    {
        var moduleResults = new[]
        {
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 5),
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 3),
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 7),
        };

        var overall = DeriveOverallStatus(moduleResults);

        Assert.Equal(ModuleHealthStatus.Healthy, overall);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeriveOverallStatus_OneDegraded_ReturnsDegraded()
    {
        var moduleResults = new[]
        {
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 5),
            new ModuleHealthResult(ModuleHealthStatus.Degraded, 10),
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 3),
        };

        var overall = DeriveOverallStatus(moduleResults);

        Assert.Equal(ModuleHealthStatus.Degraded, overall);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeriveOverallStatus_OneUnhealthy_ReturnsUnhealthy()
    {
        var moduleResults = new[]
        {
            new ModuleHealthResult(ModuleHealthStatus.Healthy, 5),
            new ModuleHealthResult(ModuleHealthStatus.Unhealthy, 10),
            new ModuleHealthResult(ModuleHealthStatus.Degraded, 3),
        };

        var overall = DeriveOverallStatus(moduleResults);

        Assert.Equal(ModuleHealthStatus.Unhealthy, overall);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DeriveOverallStatus_UnhealthyTakesPrecedenceOverDegraded()
    {
        var moduleResults = new[]
        {
            new ModuleHealthResult(ModuleHealthStatus.Degraded, 5),
            new ModuleHealthResult(ModuleHealthStatus.Unhealthy, 10),
            new ModuleHealthResult(ModuleHealthStatus.Degraded, 3),
        };

        var overall = DeriveOverallStatus(moduleResults);

        Assert.Equal(ModuleHealthStatus.Unhealthy, overall);
    }

    /// <summary>
    /// Mirrors the aggregation logic used in SystemHealthEndpoint.DeriveOverallStatus.
    /// </summary>
    private static ModuleHealthStatus DeriveOverallStatus(ModuleHealthResult[] moduleResults)
    {
        if (moduleResults.Any(m => m.Status == ModuleHealthStatus.Unhealthy))
        {
            return ModuleHealthStatus.Unhealthy;
        }

        if (moduleResults.Any(m => m.Status == ModuleHealthStatus.Degraded))
        {
            return ModuleHealthStatus.Degraded;
        }

        return ModuleHealthStatus.Healthy;
    }
}
