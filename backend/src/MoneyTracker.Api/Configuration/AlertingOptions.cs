namespace MoneyTracker.Api.Configuration;

public sealed class AlertingOptions
{
    public const string SectionName = "Alerting";

    public double ErrorRateThresholdPercent { get; init; } = 5;

    public int ErrorRateWindowSeconds { get; init; } = 300;
}
