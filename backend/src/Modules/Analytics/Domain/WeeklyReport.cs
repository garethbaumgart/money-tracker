namespace MoneyTracker.Modules.Analytics.Domain;

public sealed class WeeklyReport
{
    public Guid Id { get; }
    public DateTimeOffset PeriodStart { get; }
    public DateTimeOffset PeriodEnd { get; }
    public string ReportType { get; }
    public string Data { get; }
    public DateTimeOffset GeneratedAtUtc { get; }

    private WeeklyReport(
        Guid id,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string reportType,
        string data,
        DateTimeOffset generatedAtUtc)
    {
        Id = id;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        ReportType = reportType;
        Data = data;
        GeneratedAtUtc = generatedAtUtc;
    }

    public static WeeklyReport Create(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string reportType,
        string data,
        DateTimeOffset generatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reportType))
        {
            throw new AnalyticsDomainException(
                AnalyticsErrors.ValidationError,
                "Report type is required.");
        }

        return new WeeklyReport(
            Guid.NewGuid(),
            periodStart,
            periodEnd,
            reportType,
            data,
            generatedAtUtc);
    }
}
