namespace MoneyTracker.Modules.Insights.Domain;

public sealed record CategoryHealth(
    Guid CategoryId,
    string CategoryName,
    decimal Allocated,
    decimal Spent,
    CategoryHealthStatus Status)
{
    public static CategoryHealthStatus ComputeStatus(decimal allocated, decimal spent)
    {
        if (allocated == 0)
        {
            return spent > 0 ? CategoryHealthStatus.OverBudget : CategoryHealthStatus.OnTrack;
        }

        var ratio = (double)(spent / allocated) * 100;

        return ratio switch
        {
            > 100 => CategoryHealthStatus.OverBudget,
            >= InsightsPolicy.AtRiskThresholdPercent => CategoryHealthStatus.AtRisk,
            _ => CategoryHealthStatus.OnTrack
        };
    }
}

public sealed class BudgetHealthScore
{
    public int OverallScore { get; }
    public double AdherenceScore { get; }
    public double VelocityScore { get; }
    public double BillPaymentScore { get; }
    public IReadOnlyCollection<CategoryHealth> CategoryHealth { get; }

    private BudgetHealthScore(
        int overallScore,
        double adherenceScore,
        double velocityScore,
        double billPaymentScore,
        IReadOnlyCollection<CategoryHealth> categoryHealth)
    {
        OverallScore = overallScore;
        AdherenceScore = adherenceScore;
        VelocityScore = velocityScore;
        BillPaymentScore = billPaymentScore;
        CategoryHealth = categoryHealth;
    }

    public static BudgetHealthScore Compute(
        IReadOnlyCollection<CategoryBudgetData> categoryData,
        decimal totalAllocated,
        decimal totalSpent,
        int daysRemainingInPeriod,
        int totalDaysInPeriod,
        int billsPaidOnTime,
        int totalBillsDue)
    {
        // Adherence: % of categories where spent <= allocated
        var adherence = categoryData.Count == 0
            ? 100.0
            : (double)categoryData.Count(c => c.Spent <= c.Allocated) / categoryData.Count * 100;

        // Velocity: spending pace vs remaining days
        var daysElapsed = totalDaysInPeriod - daysRemainingInPeriod;
        double velocity;
        if (daysElapsed <= 0 || totalSpent == 0)
        {
            velocity = 100;
        }
        else
        {
            var avgDailySpend = (double)totalSpent / daysElapsed;
            var remainingBudget = (double)(totalAllocated - totalSpent);

            if (remainingBudget <= 0)
            {
                velocity = 0;
            }
            else if (avgDailySpend <= 0)
            {
                velocity = 100;
            }
            else
            {
                var daysOfBudgetLeft = remainingBudget / avgDailySpend;
                velocity = Math.Min(100, Math.Max(0, daysOfBudgetLeft / daysRemainingInPeriod * 100));
            }
        }

        // Bill payment: % of bills paid on time
        var billPayment = totalBillsDue == 0
            ? 100.0
            : (double)billsPaidOnTime / totalBillsDue * 100;

        // Composite score
        var overall = (int)Math.Round(
            adherence * 0.40 + velocity * 0.35 + billPayment * 0.25);

        // Category health
        var categoryHealth = categoryData
            .Select(c => new Domain.CategoryHealth(
                c.CategoryId,
                c.CategoryName,
                c.Allocated,
                c.Spent,
                Domain.CategoryHealth.ComputeStatus(c.Allocated, c.Spent)))
            .ToArray();

        return new BudgetHealthScore(
            overall,
            adherence,
            velocity,
            billPayment,
            categoryHealth);
    }
}

public sealed record CategoryBudgetData(
    Guid CategoryId,
    string CategoryName,
    decimal Allocated,
    decimal Spent);
