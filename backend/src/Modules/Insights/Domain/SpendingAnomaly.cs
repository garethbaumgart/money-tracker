namespace MoneyTracker.Modules.Insights.Domain;

public sealed record SpendingAnomaly(
    Guid CategoryId,
    string CategoryName,
    decimal CurrentSpent,
    decimal PreviousSpent,
    double ChangePercent)
{
    /// <summary>
    /// Detects anomalies for a category by comparing current to previous spending.
    /// Returns null if previousSpent is zero (cannot compute change) or if change
    /// does not exceed the threshold.
    /// </summary>
    public static SpendingAnomaly? Detect(
        Guid categoryId,
        string categoryName,
        decimal currentSpent,
        decimal previousSpent,
        double thresholdPercent)
    {
        if (previousSpent == 0)
        {
            return null;
        }

        var changePercent = (double)((currentSpent - previousSpent) / previousSpent) * 100;

        if (changePercent <= thresholdPercent)
        {
            return null;
        }

        return new SpendingAnomaly(categoryId, categoryName, currentSpent, previousSpent, changePercent);
    }
}
