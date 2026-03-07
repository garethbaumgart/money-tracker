namespace MoneyTracker.Modules.Analytics.Application.RecordEvent;

public sealed record RecordEventCommand(
    Guid UserId,
    string Platform,
    IReadOnlyCollection<RecordEventItem> Events);
