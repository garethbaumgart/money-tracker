using System.Diagnostics;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;

namespace MoneyTracker.Api.Observability;

internal sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger,
    IOptions<PerformanceOptions> performanceOptions,
    IHostEnvironment hostEnvironment,
    ErrorRateMonitor errorRateMonitor)
{
    private const string DurationHeader = "X-Request-Duration-Ms";

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;
        var correlationId = CorrelationHeaders.GetCorrelationId(context);

        errorRateMonitor.RecordRequest(path, statusCode, elapsedMs);

        if (!hostEnvironment.IsProduction() && !context.Response.HasStarted)
        {
            context.Response.Headers[DurationHeader] = elapsedMs.ToString();
        }

        logger.LogInformation(
            "Request completed {Method} {Path} statusCode={StatusCode} durationMs={DurationMs} correlationId={CorrelationId}",
            method,
            path,
            statusCode,
            elapsedMs,
            correlationId);

        var budgetMs = performanceOptions.Value.ResponseTimeBudgets.GetBudgetForPath(path);
        if (elapsedMs > budgetMs)
        {
            logger.LogWarning(
                "Response time budget exceeded for {Method} {Path}: actual={DurationMs}ms budget={BudgetMs}ms correlationId={CorrelationId}",
                method,
                path,
                elapsedMs,
                budgetMs,
                correlationId);
        }
    }
}
