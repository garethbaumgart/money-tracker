namespace MoneyTracker.Modules.Analytics.Domain;

public interface IRevenueDataSource
{
    Task<RevenueMetrics> GetRevenueMetricsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);
}
