using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetRevenueMetrics;

public sealed class GetRevenueMetricsHandler(
    IRevenueDataSource revenueDataSource,
    TimeProvider timeProvider)
{
    public async Task<GetRevenueMetricsResult> HandleAsync(
        GetRevenueMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var asOf = query.AsOf ?? timeProvider.GetUtcNow();

        var metrics = await revenueDataSource.GetRevenueMetricsAsync(
            asOf,
            cancellationToken);

        return GetRevenueMetricsResult.Success(metrics);
    }
}
