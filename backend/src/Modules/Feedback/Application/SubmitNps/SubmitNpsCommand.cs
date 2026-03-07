namespace MoneyTracker.Modules.Feedback.Application.SubmitNps;

public sealed record SubmitNpsCommand(
    Guid UserId,
    int Score,
    string? Comment);
