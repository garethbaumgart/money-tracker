using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.GetFeedbackSummary;

public sealed class GetFeedbackSummaryResult
{
    private GetFeedbackSummaryResult(
        FeedbackSummaryData? data,
        string? errorCode,
        string? errorMessage)
    {
        Data = data;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public FeedbackSummaryData? Data { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => Data is not null;

    public static GetFeedbackSummaryResult Success(FeedbackSummaryData data)
    {
        return new GetFeedbackSummaryResult(data, errorCode: null, errorMessage: null);
    }

    public static GetFeedbackSummaryResult Error(string code, string message)
    {
        return new GetFeedbackSummaryResult(data: null, code, message);
    }
}

public sealed record FeedbackSummaryData(
    int TotalFeedback,
    Dictionary<string, int> ByCategory,
    double AvgSatisfaction,
    double NpsScore,
    Dictionary<string, int> PriorityDistribution,
    TrendData? Trends);

public sealed record TrendData(
    int CurrentWeekCount,
    int PreviousWeekCount,
    double WowChangePercent);
