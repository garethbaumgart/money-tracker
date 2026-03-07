namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record FunnelTrends(
    double? WeekOverWeek,
    double? MonthOverMonth);
