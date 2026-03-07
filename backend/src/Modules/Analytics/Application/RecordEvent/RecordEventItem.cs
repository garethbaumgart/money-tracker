namespace MoneyTracker.Modules.Analytics.Application.RecordEvent;

public sealed record RecordEventItem(
    string Milestone,
    Guid? HouseholdId,
    Dictionary<string, string>? Metadata,
    DateTimeOffset OccurredAtUtc);
