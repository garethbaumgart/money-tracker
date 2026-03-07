namespace MoneyTracker.Modules.Analytics.Application.GenerateWeeklyReport;

public sealed class GenerateWeeklyReportResult
{
    public bool IsSuccess { get; private init; }
    public Guid? ReportId { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static GenerateWeeklyReportResult Success(Guid reportId) =>
        new()
        {
            IsSuccess = true,
            ReportId = reportId
        };

    public static GenerateWeeklyReportResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
