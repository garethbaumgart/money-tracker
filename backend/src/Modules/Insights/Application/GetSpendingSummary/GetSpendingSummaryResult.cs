using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Application.GetSpendingSummary;

public sealed class GetSpendingSummaryResult
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public SpendingAnalysis? Analysis { get; }
    public DateTimeOffset? PeriodStartUtc { get; }
    public DateTimeOffset? PeriodEndUtc { get; }

    private GetSpendingSummaryResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage,
        SpendingAnalysis? analysis,
        DateTimeOffset? periodStartUtc,
        DateTimeOffset? periodEndUtc)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Analysis = analysis;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
    }

    public static GetSpendingSummaryResult Success(
        SpendingAnalysis analysis,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc)
        => new(true, null, null, analysis, periodStartUtc, periodEndUtc);

    public static GetSpendingSummaryResult Error(string errorCode, string errorMessage)
        => new(false, errorCode, errorMessage, null, null, null);
}
