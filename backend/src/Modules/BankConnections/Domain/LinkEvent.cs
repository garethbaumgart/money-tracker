namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class LinkEvent
{
    public LinkEventId Id { get; }
    public string Institution { get; }
    public string Region { get; }
    public EventOutcome Outcome { get; }
    public long DurationMs { get; }
    public string? ErrorCategory { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    private LinkEvent(
        LinkEventId id,
        string institution,
        string region,
        EventOutcome outcome,
        long durationMs,
        string? errorCategory,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        Institution = institution;
        Region = region;
        Outcome = outcome;
        DurationMs = durationMs;
        ErrorCategory = errorCategory;
        OccurredAtUtc = occurredAtUtc;
    }

    public static LinkEvent Create(
        string institution,
        string region,
        EventOutcome outcome,
        long durationMs,
        string? errorCategory,
        DateTimeOffset occurredAtUtc)
    {
        if (string.IsNullOrWhiteSpace(institution))
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Institution is required.");
        }

        if (string.IsNullOrWhiteSpace(region))
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Region is required.");
        }

        if (durationMs < 0)
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Duration must be non-negative.");
        }

        if (outcome == EventOutcome.Failed && string.IsNullOrWhiteSpace(errorCategory))
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Error category is required for failed events.");
        }

        return new LinkEvent(
            LinkEventId.New(),
            institution.Trim(),
            region.Trim().ToUpperInvariant(),
            outcome,
            durationMs,
            errorCategory?.Trim(),
            occurredAtUtc);
    }
}
