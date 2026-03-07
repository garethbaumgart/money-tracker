using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MoneyTracker.Api.Observability;
using MoneyTracker.Modules.SharedKernel.Health;
using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Api;

public static class SystemHealthEndpoint
{
    private static readonly TimeSpan ModuleCheckTimeout = TimeSpan.FromSeconds(3);

    public static IEndpointRouteBuilder MapSystemHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoint = (RouteHandlerBuilder)app.MapGet("/admin/system-health", GetSystemHealth);
        endpoint
            .WithName("GetSystemHealth")
            .WithSummary("Get system health status.")
            .WithDescription("Returns aggregated health status for all modules, infrastructure, and API metrics. Admin access required.")
            .Produces<SystemHealthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task GetSystemHealth(HttpContext httpContext)
    {
        var authResult = await EndpointHelpers.ResolveAuthenticatedUser(httpContext);
        if (!authResult.Success)
        {
            await authResult.Problem!.ExecuteAsync(httpContext);
            return;
        }

        var adminService = httpContext.RequestServices.GetRequiredService<IAdminAccessService>();
        var isAdmin = await adminService.IsAdminAsync(
            authResult.AuthenticatedUser!.UserId,
            httpContext.RequestAborted);
        if (!isAdmin)
        {
            await EndpointHelpers.WriteProblemAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Access denied.",
                "Admin access is required to view system health.",
                SystemHealthErrors.AccessDenied,
                SystemHealthErrors.AccessDenied);
            return;
        }

        var healthChecks = httpContext.RequestServices.GetServices<IModuleHealthCheck>();
        var errorRateMonitor = httpContext.RequestServices.GetRequiredService<ErrorRateMonitor>();
        var workerTracker = httpContext.RequestServices.GetRequiredService<BackgroundWorkerHealthTracker>();

        // Run all module health checks with timeout
        var moduleResults = await RunModuleChecksAsync(healthChecks, httpContext.RequestAborted);

        // Infrastructure health
        var workerHeartbeats = workerTracker.GetAllHeartbeats();
        var workerStatuses = workerHeartbeats
            .Select(kvp => new WorkerHealthResponse(
                kvp.Key,
                workerTracker.IsHealthy(kvp.Key) ? "healthy" : "stale",
                kvp.Value))
            .ToArray();

        var databaseStatus = "connected"; // In-memory repositories are always available

        var infrastructureHealth = new InfrastructureHealthResponse(
            databaseStatus,
            workerStatuses);

        // API metrics
        var overallErrorRate = errorRateMonitor.ComputeOverallErrorRate();
        var latencyPercentiles = errorRateMonitor.ComputeLatencyPercentiles();

        var apiMetrics = new ApiMetricsResponse(
            Math.Round(overallErrorRate, 2),
            latencyPercentiles.P50Ms,
            latencyPercentiles.P95Ms,
            latencyPercentiles.P99Ms);

        // Derive overall status
        var overallStatus = DeriveOverallStatus(moduleResults, workerStatuses);

        var response = new SystemHealthResponse(
            overallStatus.ToString().ToLowerInvariant(),
            DateTimeOffset.UtcNow,
            moduleResults
                .Select(m => new ModuleHealthResponse(
                    m.ModuleName,
                    m.Result.Status.ToString().ToLowerInvariant(),
                    m.Result.LatencyMs,
                    m.Result.Details))
                .ToArray(),
            infrastructureHealth,
            apiMetrics);

        await TypedResults.Ok(response).ExecuteAsync(httpContext);
    }

    private static async Task<IReadOnlyList<ModuleCheckResult>> RunModuleChecksAsync(
        IEnumerable<IModuleHealthCheck> healthChecks,
        CancellationToken ct)
    {
        var results = new List<ModuleCheckResult>();

        foreach (var check in healthChecks)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(ModuleCheckTimeout);

                var result = await check.CheckAsync(timeoutCts.Token);
                results.Add(new ModuleCheckResult(check.ModuleName, result));
            }
            catch (OperationCanceledException)
            {
                results.Add(new ModuleCheckResult(
                    check.ModuleName,
                    new ModuleHealthResult(
                        ModuleHealthStatus.Unhealthy,
                        (long)ModuleCheckTimeout.TotalMilliseconds,
                        new Dictionary<string, object> { ["error"] = "Health check timed out" })));
            }
            catch (Exception ex)
            {
                results.Add(new ModuleCheckResult(
                    check.ModuleName,
                    new ModuleHealthResult(
                        ModuleHealthStatus.Unhealthy,
                        0,
                        new Dictionary<string, object> { ["error"] = ex.Message })));
            }
        }

        return results;
    }

    private static ModuleHealthStatus DeriveOverallStatus(
        IReadOnlyList<ModuleCheckResult> moduleResults,
        WorkerHealthResponse[] workerStatuses)
    {
        // If any module is unhealthy, overall is unhealthy
        if (moduleResults.Any(m => m.Result.Status == ModuleHealthStatus.Unhealthy))
        {
            return ModuleHealthStatus.Unhealthy;
        }

        // If any worker is stale, overall is degraded
        if (workerStatuses.Any(w => w.Status == "stale"))
        {
            return ModuleHealthStatus.Degraded;
        }

        // If any module is degraded, overall is degraded
        if (moduleResults.Any(m => m.Result.Status == ModuleHealthStatus.Degraded))
        {
            return ModuleHealthStatus.Degraded;
        }

        return ModuleHealthStatus.Healthy;
    }

    private sealed record ModuleCheckResult(string ModuleName, ModuleHealthResult Result);
}

internal static class SystemHealthErrors
{
    public const string AccessDenied = "SYSTEM_HEALTH_ACCESS_DENIED";
}

// Response DTOs
public sealed record SystemHealthResponse(
    string Status,
    DateTimeOffset CheckedAtUtc,
    ModuleHealthResponse[] Modules,
    InfrastructureHealthResponse Infrastructure,
    ApiMetricsResponse ApiMetrics);

public sealed record ModuleHealthResponse(
    string ModuleName,
    string Status,
    long LatencyMs,
    Dictionary<string, object>? Details);

public sealed record InfrastructureHealthResponse(
    string DatabaseStatus,
    WorkerHealthResponse[] BackgroundWorkers);

public sealed record WorkerHealthResponse(
    string WorkerName,
    string Status,
    DateTimeOffset LastHeartbeatUtc);

public sealed record ApiMetricsResponse(
    double ErrorRatePercent,
    long P50LatencyMs,
    long P95LatencyMs,
    long P99LatencyMs);
