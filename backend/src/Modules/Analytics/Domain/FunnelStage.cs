namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record FunnelStage(
    string Milestone,
    int UserCount,
    double ConversionRate,
    double DropOffRate);
