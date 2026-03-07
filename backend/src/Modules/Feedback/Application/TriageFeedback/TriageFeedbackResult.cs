namespace MoneyTracker.Modules.Feedback.Application.TriageFeedback;

public sealed class TriageFeedbackResult
{
    private TriageFeedbackResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static TriageFeedbackResult Success()
    {
        return new TriageFeedbackResult(true, errorCode: null, errorMessage: null);
    }

    public static TriageFeedbackResult NotFound()
    {
        return new TriageFeedbackResult(false, Domain.FeedbackErrors.NotFound, "Feedback not found.");
    }

    public static TriageFeedbackResult Validation(string code, string message)
    {
        return new TriageFeedbackResult(false, code, message);
    }
}
