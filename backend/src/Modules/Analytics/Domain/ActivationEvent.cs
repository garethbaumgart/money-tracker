namespace MoneyTracker.Modules.Analytics.Domain;

public sealed class ActivationEvent
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public ActivationMilestone Milestone { get; }
    public Guid? HouseholdId { get; }
    public string Platform { get; }
    public string? Region { get; }
    public Dictionary<string, string>? Metadata { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    private ActivationEvent(
        Guid id,
        Guid userId,
        ActivationMilestone milestone,
        Guid? householdId,
        string platform,
        string? region,
        Dictionary<string, string>? metadata,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        UserId = userId;
        Milestone = milestone;
        HouseholdId = householdId;
        Platform = platform;
        Region = region;
        Metadata = metadata;
        OccurredAtUtc = occurredAtUtc;
        RecordedAtUtc = recordedAtUtc;
    }

    public static ActivationEvent Create(
        Guid userId,
        ActivationMilestone milestone,
        Guid? householdId,
        string platform,
        string? region,
        Dictionary<string, string>? metadata,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset recordedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new AnalyticsDomainException(
                AnalyticsErrors.ValidationError,
                "User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new AnalyticsDomainException(
                AnalyticsErrors.ValidationError,
                "Platform is required.");
        }

        return new ActivationEvent(
            Guid.NewGuid(),
            userId,
            milestone,
            householdId,
            platform,
            region,
            metadata,
            occurredAtUtc,
            recordedAtUtc);
    }
}
