namespace MoneyTracker.Modules.Feedback.Domain;

public sealed class FeedbackDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
