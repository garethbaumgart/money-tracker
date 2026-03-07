namespace MoneyTracker.Modules.Analytics.Application.GetFunnelReport;

public sealed record GetFunnelReportQuery(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd);
