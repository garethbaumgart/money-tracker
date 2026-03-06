using System.Diagnostics;

namespace MoneyTracker.Api.Observability;

internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incomingCorrelationId = context.Request.Headers[CorrelationHeaders.CorrelationIdHeader].ToString();
        var correlationId = string.IsNullOrWhiteSpace(incomingCorrelationId)
            ? Guid.NewGuid().ToString("N")
            : incomingCorrelationId;

        context.Items[CorrelationHeaders.CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationHeaders.CorrelationIdHeader] = correlationId;
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        logger.LogInformation(
            "Request started {Method} {Path} correlationId={CorrelationId} traceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            traceId);

        await next(context);
    }
}

internal static class CorrelationHeaders
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "MoneyTracker.CorrelationId";

    public static string GetCorrelationId(HttpContext context)
        => context.Items[CorrelationIdItemKey]?.ToString() ?? context.TraceIdentifier;
}
