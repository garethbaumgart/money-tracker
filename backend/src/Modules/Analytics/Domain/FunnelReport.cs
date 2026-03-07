namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record FunnelReport(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    IReadOnlyList<FunnelReportStage> Stages,
    double OverallConversion,
    IReadOnlyList<DropOffAnalysis> TopDropOffs,
    FunnelTrends Trends);
