namespace MoneyTracker.Modules.Feedback.Domain;

public readonly record struct FeedbackId(Guid Value)
{
    public static FeedbackId New() => new(Guid.NewGuid());
}
