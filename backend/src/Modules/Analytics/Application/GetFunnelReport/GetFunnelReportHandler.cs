using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetFunnelReport;

public sealed class GetFunnelReportHandler(IFunnelDataSource funnelDataSource)
{
    public async Task<GetFunnelReportResult> HandleAsync(
        GetFunnelReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PeriodStart >= query.PeriodEnd)
        {
            return GetFunnelReportResult.Failure(
                AnalyticsErrors.ValidationError,
                "periodStart must be before periodEnd.");
        }

        var report = await funnelDataSource.GetFunnelReportAsync(
            query.PeriodStart,
            query.PeriodEnd,
            cancellationToken);

        return GetFunnelReportResult.Success(report);
    }
}
