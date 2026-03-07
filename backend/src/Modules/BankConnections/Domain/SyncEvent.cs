namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class SyncEvent
{
    public SyncEventId Id { get; }
    public BankConnectionId ConnectionId { get; }
    public string Institution { get; }
    public string Region { get; }
    public EventOutcome Outcome { get; }
    public long DurationMs { get; }
    public int TransactionCount { get; }
    public string? ErrorCategory { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    private SyncEvent(
        SyncEventId id,
        BankConnectionId connectionId,
        string institution,
        string region,
        EventOutcome outcome,
        long durationMs,
        int transactionCount,
        string? errorCategory,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        ConnectionId = connectionId;
        Institution = institution;
        Region = region;
        Outcome = outcome;
        DurationMs = durationMs;
        TransactionCount = transactionCount;
        ErrorCategory = errorCategory;
        OccurredAtUtc = occurredAtUtc;
    }

    public static SyncEvent Create(
        BankConnectionId connectionId,
        string institution,
        string region,
        EventOutcome outcome,
        long durationMs,
        int transactionCount,
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

        if (transactionCount < 0)
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Transaction count must be non-negative.");
        }

        if (outcome == EventOutcome.Failed && string.IsNullOrWhiteSpace(errorCategory))
        {
            throw new BankConnectionDomainException(
                PilotMetricErrors.ValidationError,
                "Error category is required for failed events.");
        }

        return new SyncEvent(
            SyncEventId.New(),
            connectionId,
            institution.Trim(),
            region.Trim().ToUpperInvariant(),
            outcome,
            durationMs,
            transactionCount,
            errorCategory?.Trim(),
            occurredAtUtc);
    }
}
