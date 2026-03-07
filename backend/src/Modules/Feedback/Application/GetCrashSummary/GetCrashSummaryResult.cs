namespace MoneyTracker.Modules.Feedback.Application.GetCrashSummary;

public sealed class GetCrashSummaryResult
{
    private GetCrashSummaryResult(
        CrashSummaryData? data,
        string? errorCode,
        string? errorMessage)
    {
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public CrashSummaryData? Data { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => Data is not null;

    public static GetCrashSummaryResult Success(CrashSummaryData data)
    {
        return new GetCrashSummaryResult(data, errorCode: null, errorMessage: null);
    }

    public static GetCrashSummaryResult Error(string code, string message)
    {
        return new GetCrashSummaryResult(data: null, code, message);
    }
}

public sealed record CrashSummaryData(
    double CrashFreeRate,
    int TotalCrashes,
    CrashReportSummary[] TopCrashes);

public sealed record CrashReportSummary(
    string Signature,
    int Count,
    int AffectedUsers,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen);
