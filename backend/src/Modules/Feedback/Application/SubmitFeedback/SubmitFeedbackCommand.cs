using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.SubmitFeedback;

public sealed record SubmitFeedbackCommand(
    Guid UserId,
    FeedbackCategory Category,
    string Description,
    int Rating,
    string? ScreenName,
    string? AppVersion,
    string? DeviceModel,
    string? OsVersion,
    string UserTier);
