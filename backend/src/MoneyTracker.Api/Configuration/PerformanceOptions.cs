namespace MoneyTracker.Api.Configuration;

public sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    public ResponseTimeBudgetsOptions ResponseTimeBudgets { get; init; } = new();

    public ConnectionPoolOptions ConnectionPool { get; init; } = new();
}

public sealed class ResponseTimeBudgetsOptions
{
    public int Auth { get; init; } = 200;

    public int Crud { get; init; } = 300;

    public int Dashboard { get; init; } = 500;

    public int Insights { get; init; } = 500;

    public int BankSync { get; init; } = 1000;

    public int Admin { get; init; } = 1000;

    /// <summary>
    /// Returns the response time budget in milliseconds for the given request path.
    /// Falls back to the CRUD budget when no specific group matches.
    /// </summary>
    public int GetBudgetForPath(string path)
    {
        if (path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
        {
            return Auth;
        }

        if (path.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return Dashboard;
        }

        if (path.StartsWith("/insights", StringComparison.OrdinalIgnoreCase))
        {
            return Insights;
        }

        if (path.StartsWith("/bank-connections", StringComparison.OrdinalIgnoreCase))
        {
            return BankSync;
        }

        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        return Crud;
    }
}

public sealed class ConnectionPoolOptions
{
    public int MinPoolSize { get; init; } = 5;

    public int MaxPoolSize { get; init; } = 50;

    public int ConnectionIdleLifetime { get; init; } = 300;

    public int ConnectionLifetime { get; init; } = 3600;
}
