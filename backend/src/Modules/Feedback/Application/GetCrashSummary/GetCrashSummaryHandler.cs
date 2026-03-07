using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.GetCrashSummary;

public sealed class GetCrashSummaryHandler(
    ICrashReportRepository crashReportRepository)
{
    // Placeholder total sessions count for crash-free rate calculation.
    // In production, this would come from an analytics/telemetry source.
    private const int EstimatedTotalSessions = 10000;

    public async Task<GetCrashSummaryResult> HandleAsync(
        GetCrashSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var reports = await crashReportRepository.GetRecentAsync(
            query.PeriodDays,
            cancellationToken);

        var totalCrashes = await crashReportRepository.GetTotalCrashCountAsync(
            query.PeriodDays,
            cancellationToken);

        var crashFreeRate = EstimatedTotalSessions > 0
            ? Math.Max(0, 1.0 - ((double)totalCrashes / EstimatedTotalSessions)) * 100
            : 100.0;

        var topCrashes = reports
            .Take(10)
            .Select(r => new CrashReportSummary(
                r.Signature,
                r.Count,
                r.AffectedUsers,
                r.FirstSeen,
                r.LastSeen))
            .ToArray();

        var data = new CrashSummaryData(
            crashFreeRate,
            totalCrashes,
            topCrashes);

        return GetCrashSummaryResult.Success(data);
    }
}
