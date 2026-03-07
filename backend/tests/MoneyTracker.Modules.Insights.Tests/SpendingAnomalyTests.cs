using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Tests;

public class SpendingAnomalyTests
{
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Detect_ExceedsThreshold_ReturnsAnomaly()
    {
        var result = SpendingAnomaly.Detect(
            CategoryId, "Groceries", 300m, 100m, 50);

        Assert.NotNull(result);
        Assert.Equal(CategoryId, result.CategoryId);
        Assert.Equal("Groceries", result.CategoryName);
        Assert.Equal(300m, result.CurrentSpent);
        Assert.Equal(100m, result.PreviousSpent);
        Assert.Equal(200.0, result.ChangePercent, 0.01);
    }

    [Fact]
    public void Detect_BelowThreshold_ReturnsNull()
    {
        var result = SpendingAnomaly.Detect(
            CategoryId, "Groceries", 140m, 100m, 50);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_ExactlyAtThreshold_ReturnsNull()
    {
        // 150 is exactly 50% above 100, threshold is 50% — NOT exceeding, should return null
        var result = SpendingAnomaly.Detect(
            CategoryId, "Groceries", 150m, 100m, 50);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_ZeroPreviousSpend_ReturnsNull()
    {
        var result = SpendingAnomaly.Detect(
            CategoryId, "Groceries", 500m, 0m, 50);

        Assert.Null(result);
    }

    [Fact]
    public void Detect_SpendingDecreased_ReturnsNull()
    {
        var result = SpendingAnomaly.Detect(
            CategoryId, "Groceries", 50m, 100m, 50);

        Assert.Null(result);
    }
}
