using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetRetentionCohorts;

public sealed class GetRetentionCohortsHandler(
    IRetentionDataSource retentionDataSource,
    TimeProvider timeProvider)
{
    public async Task<GetRetentionCohortsResult> HandleAsync(
        GetRetentionCohortsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.CohortCount <= 0)
        {
            return GetRetentionCohortsResult.Failure(
                AnalyticsErrors.ValidationError,
                "cohortCount must be greater than 0.");
        }

        var asOfUtc = timeProvider.GetUtcNow();

        var cohorts = await retentionDataSource.GetRetentionCohortsAsync(
            query.CohortCount,
            asOfUtc,
            cancellationToken);

        return GetRetentionCohortsResult.Success(cohorts);
    }
}
