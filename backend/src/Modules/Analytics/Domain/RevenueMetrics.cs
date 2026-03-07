namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record RevenueMetrics(
    decimal Mrr,
    decimal Arpu,
    double ChurnRate,
    decimal? EstimatedLtv,
    int ActiveSubscribers,
    int TrialUsers,
    FunnelTrends Trends);
