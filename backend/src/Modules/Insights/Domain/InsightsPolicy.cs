namespace MoneyTracker.Modules.Insights.Domain;

public static class InsightsPolicy
{
    public const double DefaultAnomalyThresholdPercent = 50;
    public const double AtRiskThresholdPercent = 80;
    public const string FeatureKey = "PremiumInsights";
}
