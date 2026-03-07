using System.Text.Json;
using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GenerateWeeklyReport;

public sealed class GenerateWeeklyReportHandler(
    IFunnelDataSource funnelDataSource,
    IRetentionDataSource retentionDataSource,
    IRevenueDataSource revenueDataSource,
    IWeeklyReportRepository weeklyReportRepository,
    TimeProvider timeProvider)
{
    public async Task<GenerateWeeklyReportResult> HandleAsync(
        GenerateWeeklyReportCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var periodEnd = nowUtc;
        var periodStart = nowUtc.AddDays(-7);

        var funnel = await funnelDataSource.GetFunnelReportAsync(
            periodStart, periodEnd, cancellationToken);

        var cohorts = await retentionDataSource.GetRetentionCohortsAsync(
            8, nowUtc, cancellationToken);

        var revenue = await revenueDataSource.GetRevenueMetricsAsync(
            nowUtc, cancellationToken);

        var reportData = new
        {
            Funnel = funnel,
            Cohorts = cohorts,
            Revenue = revenue
        };

        var dataJson = JsonSerializer.Serialize(reportData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var report = WeeklyReport.Create(
            periodStart,
            periodEnd,
            "weekly_summary",
            dataJson,
            nowUtc);

        await weeklyReportRepository.AddAsync(report, cancellationToken);

        return GenerateWeeklyReportResult.Success(report.Id);
    }
}
