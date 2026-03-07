namespace MoneyTracker.Modules.Insights.Domain;

public sealed record CategorySpending(
    Guid CategoryId,
    string CategoryName,
    decimal CurrentSpent,
    decimal PreviousSpent,
    double ChangePercent);

public sealed record TopCategory(
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    double PercentOfTotal);

public sealed class SpendingAnalysis
{
    public decimal TotalSpent { get; }
    public decimal PreviousPeriodTotalSpent { get; }
    public double SpendingChangePercent { get; }
    public IReadOnlyCollection<CategorySpending> Categories { get; }
    public IReadOnlyCollection<TopCategory> TopCategories { get; }
    public IReadOnlyCollection<SpendingAnomaly> Anomalies { get; }

    private SpendingAnalysis(
        decimal totalSpent,
        decimal previousPeriodTotalSpent,
        double spendingChangePercent,
        IReadOnlyCollection<CategorySpending> categories,
        IReadOnlyCollection<TopCategory> topCategories,
        IReadOnlyCollection<SpendingAnomaly> anomalies)
    {
        TotalSpent = totalSpent;
        PreviousPeriodTotalSpent = previousPeriodTotalSpent;
        SpendingChangePercent = spendingChangePercent;
        Categories = categories;
        TopCategories = topCategories;
        Anomalies = anomalies;
    }

    public static SpendingAnalysis Compute(
        IReadOnlyCollection<CategorySpendingData> currentPeriodData,
        IReadOnlyCollection<CategorySpendingData> previousPeriodData,
        double anomalyThresholdPercent)
    {
        var previousLookup = previousPeriodData
            .ToDictionary(d => d.CategoryId, d => d.TotalSpent);

        var totalSpent = currentPeriodData.Sum(d => d.TotalSpent);
        var previousTotalSpent = previousPeriodData.Sum(d => d.TotalSpent);
        var overallChange = previousTotalSpent == 0
            ? 0.0
            : (double)((totalSpent - previousTotalSpent) / previousTotalSpent) * 100;

        var categories = new List<CategorySpending>();
        var anomalies = new List<SpendingAnomaly>();

        foreach (var current in currentPeriodData)
        {
            var previousSpent = previousLookup.GetValueOrDefault(current.CategoryId, 0m);
            var changePercent = previousSpent == 0
                ? 0.0
                : (double)((current.TotalSpent - previousSpent) / previousSpent) * 100;

            categories.Add(new CategorySpending(
                current.CategoryId,
                current.CategoryName,
                current.TotalSpent,
                previousSpent,
                changePercent));

            var anomaly = SpendingAnomaly.Detect(
                current.CategoryId,
                current.CategoryName,
                current.TotalSpent,
                previousSpent,
                anomalyThresholdPercent);

            if (anomaly is not null)
            {
                anomalies.Add(anomaly);
            }
        }

        var topCategories = currentPeriodData
            .OrderByDescending(d => d.TotalSpent)
            .Select(d => new TopCategory(
                d.CategoryId,
                d.CategoryName,
                d.TotalSpent,
                totalSpent == 0 ? 0.0 : (double)(d.TotalSpent / totalSpent) * 100))
            .ToArray();

        return new SpendingAnalysis(
            totalSpent,
            previousTotalSpent,
            overallChange,
            categories.AsReadOnly(),
            topCategories,
            anomalies.AsReadOnly());
    }
}

public sealed record CategorySpendingData(
    Guid CategoryId,
    string CategoryName,
    decimal TotalSpent);
