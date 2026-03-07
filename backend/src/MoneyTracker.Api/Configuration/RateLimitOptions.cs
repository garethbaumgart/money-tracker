namespace MoneyTracker.Api.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitGroupOptions Auth { get; init; } = new() { RequestsPerMinute = 10, KeyType = "IP" };
    public RateLimitGroupOptions Crud { get; init; } = new() { RequestsPerMinute = 60, KeyType = "User" };
    public RateLimitGroupOptions Webhooks { get; init; } = new() { RequestsPerMinute = 100, KeyType = "IP" };
    public RateLimitGroupOptions Admin { get; init; } = new() { RequestsPerMinute = 30, KeyType = "User" };
    public RateLimitGroupOptions Bank { get; init; } = new() { RequestsPerMinute = 20, KeyType = "User" };
    public RateLimitGroupOptions Insights { get; init; } = new() { RequestsPerMinute = 30, KeyType = "User" };
    public RateLimitGroupOptions Analytics { get; init; } = new() { RequestsPerMinute = 60, KeyType = "User" };

    public RateLimitGroupOptions GetGroupForPath(string path)
    {
        if (path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
            return Auth;

        if (path.StartsWith("/webhooks", StringComparison.OrdinalIgnoreCase))
            return Webhooks;

        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            return Admin;

        if (path.StartsWith("/bank-connections", StringComparison.OrdinalIgnoreCase))
            return Bank;

        if (path.StartsWith("/insights", StringComparison.OrdinalIgnoreCase))
            return Insights;

        if (path.StartsWith("/analytics", StringComparison.OrdinalIgnoreCase))
            return Analytics;

        return Crud;
    }
}

public sealed class RateLimitGroupOptions
{
    public int RequestsPerMinute { get; init; }
    public string KeyType { get; init; } = "User";
}
