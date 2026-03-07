namespace MoneyTracker.Modules.Analytics.Domain;

public interface IFunnelDataSource
{
    Task<FunnelReport> GetFunnelReportAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}
