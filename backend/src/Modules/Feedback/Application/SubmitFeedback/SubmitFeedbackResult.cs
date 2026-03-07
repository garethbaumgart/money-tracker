namespace MoneyTracker.Modules.Feedback.Application.SubmitFeedback;

public sealed class SubmitFeedbackResult
{
    private SubmitFeedbackResult(
        Guid? feedbackId,
        string? status,
        string? errorCode,
        string? errorMessage)
    {
        FeedbackId = feedbackId;
        Status = status;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Guid? FeedbackId { get; }
    public string? Status { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => FeedbackId is not null;

    public static SubmitFeedbackResult Success(Guid feedbackId, string status)
    {
        return new SubmitFeedbackResult(feedbackId, status, errorCode: null, errorMessage: null);
    }

    public static SubmitFeedbackResult Validation(string code, string message)
    {
        return new SubmitFeedbackResult(feedbackId: null, status: null, code, message);
    }

    public static SubmitFeedbackResult RateLimited()
    {
        return new SubmitFeedbackResult(
            feedbackId: null,
            status: null,
            Domain.FeedbackErrors.RateLimitExceeded,
            "Maximum of 5 feedback submissions per 24 hours exceeded.");
    }
}
