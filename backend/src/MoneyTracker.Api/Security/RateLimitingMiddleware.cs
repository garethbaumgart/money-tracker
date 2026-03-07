using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;

namespace MoneyTracker.Api.Security;

internal sealed class RateLimitingMiddleware(
    RequestDelegate next,
    IOptions<RateLimitOptions> rateLimitOptions)
{
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var group = rateLimitOptions.Value.GetGroupForPath(path);

        var key = ResolveKey(context, group.KeyType);
        var counter = _counters.GetOrAdd(key, _ => new SlidingWindowCounter());

        if (!counter.TryIncrement(group.RequestsPerMinute))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = 429,
                title = "Too many requests.",
                detail = "Rate limit exceeded. Please retry after the indicated period.",
                code = "rate_limit_exceeded"
            });
            return;
        }

        await next(context);
    }

    private static string ResolveKey(HttpContext context, string keyType)
    {
        var path = context.Request.Path.Value ?? "/";
        var groupPrefix = GetGroupPrefix(path);

        if (keyType.Equals("IP", StringComparison.OrdinalIgnoreCase))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ip}:{groupPrefix}";
        }

        // User-based key: extract from claims or fallback to IP
        var userId = context.User?.FindFirst("sub")?.Value
                     ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ip}:{groupPrefix}";
        }

        return $"user:{userId}:{groupPrefix}";
    }

    private static string GetGroupPrefix(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[0].ToLowerInvariant() : "default";
    }
}

internal sealed class SlidingWindowCounter
{
    private readonly object _sync = new();
    private readonly Queue<DateTimeOffset> _timestamps = new();

    public bool TryIncrement(int maxRequests)
    {
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddMinutes(-1);

        lock (_sync)
        {
            while (_timestamps.Count > 0 && _timestamps.Peek() < windowStart)
            {
                _timestamps.Dequeue();
            }

            if (_timestamps.Count >= maxRequests)
            {
                return false;
            }

            _timestamps.Enqueue(now);
            return true;
        }
    }
}
