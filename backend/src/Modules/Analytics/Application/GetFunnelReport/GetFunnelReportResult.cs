using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetFunnelReport;

public sealed class GetFunnelReportResult
{
    public bool IsSuccess { get; private init; }
    public FunnelReport? Report { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static GetFunnelReportResult Success(FunnelReport report) =>
        new()
        {
            IsSuccess = true,
            Report = report
        };

    public static GetFunnelReportResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
