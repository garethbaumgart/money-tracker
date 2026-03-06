using System.Diagnostics;

namespace MoneyTracker.Api.Observability;

internal sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationHeaders.TryGetIncomingCorrelationId(context, out var incomingCorrelationId)
            ? incomingCorrelationId
            : Guid.NewGuid().ToString("N");

        context.Items[CorrelationHeaders.CorrelationIdItemKey] = correlationId;
        context.Response.Headers[CorrelationHeaders.CorrelationIdHeader] = correlationId;
        var traceId = CorrelationHeaders.GetTraceId(context);

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
    private const int MaxCorrelationIdLength = 128;

    public static string GetCorrelationId(HttpContext context)
        => context.Items[CorrelationIdItemKey]?.ToString() ?? context.TraceIdentifier;

    public static string GetTraceId(HttpContext context)
        => Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

    public static bool TryGetIncomingCorrelationId(HttpContext context, out string correlationId)
    {
        correlationId = string.Empty;

        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var values))
        {
            return false;
        }

        if (values.Count != 1)
        {
            return false;
        }

        var candidate = values[0]?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (candidate.Length > MaxCorrelationIdLength)
        {
            return false;
        }

        correlationId = candidate;
        return true;
    }
}
