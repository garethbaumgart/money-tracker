namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record FunnelReportStage(
    string Name,
    int Count,
    double ConversionRate,
    double DropOffRate);
