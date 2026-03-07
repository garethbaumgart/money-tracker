using Microsoft.Extensions.Options;

namespace MoneyTracker.Api.Configuration;

internal sealed class PerformanceOptionsValidator : IValidateOptions<PerformanceOptions>
{
    public ValidateOptionsResult Validate(string? name, PerformanceOptions options)
    {
        var failures = new List<string>();

        ValidateBudgets(options.ResponseTimeBudgets, failures);
        ValidateConnectionPool(options.ConnectionPool, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateBudgets(ResponseTimeBudgetsOptions budgets, List<string> failures)
    {
        if (budgets.Auth <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:Auth must be greater than 0.");
        }

        if (budgets.Crud <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:Crud must be greater than 0.");
        }

        if (budgets.Dashboard <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:Dashboard must be greater than 0.");
        }

        if (budgets.Insights <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:Insights must be greater than 0.");
        }

        if (budgets.BankSync <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:BankSync must be greater than 0.");
        }

        if (budgets.Admin <= 0)
        {
            failures.Add("Performance:ResponseTimeBudgets:Admin must be greater than 0.");
        }
    }

    private static void ValidateConnectionPool(ConnectionPoolOptions pool, List<string> failures)
    {
        if (pool.MinPoolSize < 1)
        {
            failures.Add("Performance:ConnectionPool:MinPoolSize must be at least 1.");
        }

        if (pool.MaxPoolSize < 1)
        {
            failures.Add("Performance:ConnectionPool:MaxPoolSize must be at least 1.");
        }

        if (pool.MinPoolSize > pool.MaxPoolSize)
        {
            failures.Add("Performance:ConnectionPool:MinPoolSize must not exceed MaxPoolSize.");
        }

        if (pool.ConnectionIdleLifetime <= 0)
        {
            failures.Add("Performance:ConnectionPool:ConnectionIdleLifetime must be greater than 0.");
        }

        if (pool.ConnectionLifetime <= 0)
        {
            failures.Add("Performance:ConnectionPool:ConnectionLifetime must be greater than 0.");
        }
    }
}
