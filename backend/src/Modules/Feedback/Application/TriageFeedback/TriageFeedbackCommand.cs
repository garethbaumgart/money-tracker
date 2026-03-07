using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.TriageFeedback;

public sealed record TriageFeedbackCommand(
    FeedbackId FeedbackId,
    FeedbackStatus Status,
    double? PriorityOverride);
