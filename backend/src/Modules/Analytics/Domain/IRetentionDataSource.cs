namespace MoneyTracker.Modules.Analytics.Domain;

public interface IRetentionDataSource
{
    Task<IReadOnlyList<CohortRetention>> GetRetentionCohortsAsync(
        int cohortCount,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);
}
