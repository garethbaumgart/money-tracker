namespace MoneyTracker.Modules.Feedback.Domain;

public sealed record FeedbackMetadata(
    string? ScreenName,
    string? AppVersion,
    string? DeviceModel,
    string? OsVersion);
