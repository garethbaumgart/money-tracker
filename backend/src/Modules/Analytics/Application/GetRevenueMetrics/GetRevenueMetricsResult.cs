using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetRevenueMetrics;

public sealed class GetRevenueMetricsResult
{
    public bool IsSuccess { get; private init; }
    public RevenueMetrics? Metrics { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static GetRevenueMetricsResult Success(RevenueMetrics metrics) =>
        new()
        {
            IsSuccess = true,
            Metrics = metrics
        };

    public static GetRevenueMetricsResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
