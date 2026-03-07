using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Application.GetBudgetHealth;

public sealed class GetBudgetHealthResult
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public BudgetHealthScore? HealthScore { get; }
    public DateTimeOffset? PeriodStartUtc { get; }
    public DateTimeOffset? PeriodEndUtc { get; }

    private GetBudgetHealthResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage,
        BudgetHealthScore? healthScore,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        HealthScore = healthScore;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
    }

    public static GetBudgetHealthResult Success(
        BudgetHealthScore healthScore,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc)
        => new(true, null, null, healthScore, periodStartUtc, periodEndUtc);

    public static GetBudgetHealthResult Error(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage, null, null, null);
}
