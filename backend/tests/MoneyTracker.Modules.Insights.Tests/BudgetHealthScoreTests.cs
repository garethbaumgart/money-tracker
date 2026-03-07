using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Tests;

public class BudgetHealthScoreTests
{
    private static readonly Guid Cat1 = Guid.NewGuid();
    private static readonly Guid Cat2 = Guid.NewGuid();
    private static readonly Guid Cat3 = Guid.NewGuid();

    [Fact]
    public void Compute_WeightedAverageCorrect()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 500m),  // within budget
            new CategoryBudgetData(Cat2, "Dining", 500m, 600m),      // over budget
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1500m,
            totalSpent: 1100m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 3,
            totalBillsDue: 4);

        // Adherence: 1/2 * 100 = 50.0
        // Velocity: days elapsed = 15, avg daily = 1100/15 = 73.33, remaining budget = 400, days of budget left = 400/73.33 = 5.45, velocity = 5.45/15*100 = 36.36
        // BillPayment: 3/4 * 100 = 75.0
        // Overall = 50*0.40 + 36.36*0.35 + 75*0.25 = 20 + 12.73 + 18.75 = 51.48 -> 51

        Assert.Equal(50.0, result.AdherenceScore, 0.01);
        Assert.Equal(75.0, result.BillPaymentScore, 0.01);
        Assert.InRange(result.OverallScore, 45, 55);
    }

    [Fact]
    public void Compute_ZeroAvgDailySpend_VelocityIs100()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 0m)
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1000m,
            totalSpent: 0m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 0,
            totalBillsDue: 0);

        Assert.Equal(100.0, result.VelocityScore, 0.01);
    }

    [Fact]
    public void Compute_NoBillsDue_BillPaymentScoreIs100()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 500m)
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1000m,
            totalSpent: 500m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 0,
            totalBillsDue: 0);

        Assert.Equal(100.0, result.BillPaymentScore, 0.01);
    }

    [Fact]
    public void Compute_NoCategories_AdherenceIs100()
    {
        var result = BudgetHealthScore.Compute(
            Array.Empty<CategoryBudgetData>(),
            totalAllocated: 0m,
            totalSpent: 0m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 0,
            totalBillsDue: 0);

        Assert.Equal(100.0, result.AdherenceScore, 0.01);
    }

    [Fact]
    public void CategoryHealth_OnTrack_WhenSpentLessThan80Percent()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 1000m, spent: 700m);
        Assert.Equal(CategoryHealthStatus.OnTrack, status);
    }

    [Fact]
    public void CategoryHealth_AtRisk_WhenSpentAt80Percent()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 1000m, spent: 800m);
        Assert.Equal(CategoryHealthStatus.AtRisk, status);
    }

    [Fact]
    public void CategoryHealth_AtRisk_WhenSpentAt100Percent()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 1000m, spent: 1000m);
        Assert.Equal(CategoryHealthStatus.AtRisk, status);
    }

    [Fact]
    public void CategoryHealth_OverBudget_WhenSpentExceedsAllocated()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 1000m, spent: 1100m);
        Assert.Equal(CategoryHealthStatus.OverBudget, status);
    }

    [Fact]
    public void CategoryHealth_ZeroAllocation_SpentPositive_IsOverBudget()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 0m, spent: 100m);
        Assert.Equal(CategoryHealthStatus.OverBudget, status);
    }

    [Fact]
    public void CategoryHealth_ZeroAllocation_ZeroSpent_IsOnTrack()
    {
        var status = CategoryHealth.ComputeStatus(allocated: 0m, spent: 0m);
        Assert.Equal(CategoryHealthStatus.OnTrack, status);
    }

    [Fact]
    public void Compute_AllCategoriesWithinBudget_AdherenceIs100()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 500m),
            new CategoryBudgetData(Cat2, "Dining", 500m, 300m),
            new CategoryBudgetData(Cat3, "Transport", 300m, 200m),
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1800m,
            totalSpent: 1000m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 2,
            totalBillsDue: 2);

        Assert.Equal(100.0, result.AdherenceScore, 0.01);
    }

    [Fact]
    public void Compute_OverspentBudget_VelocityIsZero()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 1200m)
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1000m,
            totalSpent: 1200m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 0,
            totalBillsDue: 0);

        Assert.Equal(0.0, result.VelocityScore, 0.01);
    }

    [Fact]
    public void Compute_CategoryHealthListPopulated()
    {
        var categories = new[]
        {
            new CategoryBudgetData(Cat1, "Groceries", 1000m, 500m),
            new CategoryBudgetData(Cat2, "Dining", 500m, 600m),
        };

        var result = BudgetHealthScore.Compute(
            categories,
            totalAllocated: 1500m,
            totalSpent: 1100m,
            daysRemainingInPeriod: 15,
            totalDaysInPeriod: 30,
            billsPaidOnTime: 0,
            totalBillsDue: 0);

        Assert.Equal(2, result.CategoryHealth.Count);
        var groceries = result.CategoryHealth.First(c => c.CategoryId == Cat1);
        Assert.Equal(CategoryHealthStatus.OnTrack, groceries.Status);
        var dining = result.CategoryHealth.First(c => c.CategoryId == Cat2);
        Assert.Equal(CategoryHealthStatus.OverBudget, dining.Status);
    }
}
