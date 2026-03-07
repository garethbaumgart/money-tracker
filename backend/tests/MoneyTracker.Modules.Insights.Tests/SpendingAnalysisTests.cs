using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Tests;

public class SpendingAnalysisTests
{
    private static readonly Guid GroceriesCategoryId = Guid.NewGuid();
    private static readonly Guid DiningCategoryId = Guid.NewGuid();
    private static readonly Guid TransportCategoryId = Guid.NewGuid();

    [Fact]
    public void Compute_WithCategoryTotals_ReturnsCategorySpendingSums()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 500m),
            new CategorySpendingData(DiningCategoryId, "Dining", 200m)
        };
        var previousData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 400m),
            new CategorySpendingData(DiningCategoryId, "Dining", 150m)
        };

        var result = SpendingAnalysis.Compute(currentData, previousData, 50);

        Assert.Equal(700m, result.TotalSpent);
        Assert.Equal(550m, result.PreviousPeriodTotalSpent);
        Assert.Equal(2, result.Categories.Count);
        var groceries = result.Categories.First(c => c.CategoryId == GroceriesCategoryId);
        Assert.Equal(500m, groceries.CurrentSpent);
    }

    [Fact]
    public void Compute_PeriodOverPeriodComparison_CorrectChangePercentages()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 600m)
        };
        var previousData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 400m)
        };

        var result = SpendingAnalysis.Compute(currentData, previousData, 50);

        var groceries = result.Categories.First(c => c.CategoryId == GroceriesCategoryId);
        Assert.Equal(50.0, groceries.ChangePercent, 0.01);
        Assert.Equal(50.0, result.SpendingChangePercent, 0.01);
    }

    [Fact]
    public void Compute_WithAnomalyAboveThreshold_DetectsAnomaly()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 900m),
            new CategorySpendingData(DiningCategoryId, "Dining", 200m)
        };
        var previousData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 400m),
            new CategorySpendingData(DiningCategoryId, "Dining", 180m)
        };

        var result = SpendingAnalysis.Compute(currentData, previousData, 50);

        Assert.Single(result.Anomalies);
        var anomaly = result.Anomalies.First();
        Assert.Equal(GroceriesCategoryId, anomaly.CategoryId);
        Assert.Equal(900m, anomaly.CurrentSpent);
        Assert.Equal(400m, anomaly.PreviousSpent);
        Assert.Equal(125.0, anomaly.ChangePercent, 0.01);
    }

    [Fact]
    public void Compute_TopCategories_RankedByAmountDescending()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 500m),
            new CategorySpendingData(DiningCategoryId, "Dining", 200m),
            new CategorySpendingData(TransportCategoryId, "Transport", 800m)
        };

        var result = SpendingAnalysis.Compute(currentData, Array.Empty<CategorySpendingData>(), 50);

        Assert.Equal(3, result.TopCategories.Count);
        Assert.Equal(TransportCategoryId, result.TopCategories.First().CategoryId);
        Assert.Equal(GroceriesCategoryId, result.TopCategories.ElementAt(1).CategoryId);
        Assert.Equal(DiningCategoryId, result.TopCategories.Last().CategoryId);
    }

    [Fact]
    public void Compute_TopCategories_CorrectPercentOfTotal()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 750m),
            new CategorySpendingData(DiningCategoryId, "Dining", 250m)
        };

        var result = SpendingAnalysis.Compute(currentData, Array.Empty<CategorySpendingData>(), 50);

        var groceries = result.TopCategories.First(c => c.CategoryId == GroceriesCategoryId);
        var dining = result.TopCategories.First(c => c.CategoryId == DiningCategoryId);
        Assert.Equal(75.0, groceries.PercentOfTotal, 0.01);
        Assert.Equal(25.0, dining.PercentOfTotal, 0.01);
    }

    [Fact]
    public void Compute_EmptyCurrentData_ReturnsZeroTotals()
    {
        var result = SpendingAnalysis.Compute(
            Array.Empty<CategorySpendingData>(),
            Array.Empty<CategorySpendingData>(),
            50);

        Assert.Equal(0m, result.TotalSpent);
        Assert.Equal(0m, result.PreviousPeriodTotalSpent);
        Assert.Equal(0.0, result.SpendingChangePercent);
        Assert.Empty(result.Categories);
        Assert.Empty(result.TopCategories);
        Assert.Empty(result.Anomalies);
    }

    [Fact]
    public void Compute_ZeroPreviousTotal_SpendingChangePercentIsZero()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 500m)
        };

        var result = SpendingAnalysis.Compute(
            currentData,
            Array.Empty<CategorySpendingData>(),
            50);

        Assert.Equal(0.0, result.SpendingChangePercent);
    }

    [Fact]
    public void Compute_CategoryOnlyInCurrentPeriod_PreviousSpentIsZero()
    {
        var currentData = new[]
        {
            new CategorySpendingData(GroceriesCategoryId, "Groceries", 500m)
        };

        var result = SpendingAnalysis.Compute(
            currentData,
            Array.Empty<CategorySpendingData>(),
            50);

        var groceries = result.Categories.First();
        Assert.Equal(0m, groceries.PreviousSpent);
        Assert.Equal(0.0, groceries.ChangePercent);
    }
}
