using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class RevenueCalculator(
    ISubscriptionRepository subscriptionRepository) : IRevenueDataSource
{
    // Known product price mappings (monthly-equivalent amounts).
    // In a production system, these would come from a product catalog.
    private static readonly Dictionary<string, (decimal MonthlyAmount, bool IsAnnual)> ProductPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mt_premium_monthly"] = (9.99m, false),
        ["mt_premium_annual"] = (99.99m, true),
    };

    private const decimal DefaultMonthlyPrice = 9.99m;

    public async Task<RevenueMetrics> GetRevenueMetricsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var allSubscriptions = await subscriptionRepository.GetAllAsync(cancellationToken);

        var activeSubscriptions = allSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToArray();

        var trialSubscriptions = allSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Trial)
            .ToArray();

        var activeSubscribers = activeSubscriptions.Length;
        var trialUsers = trialSubscriptions.Length;

        // MRR: Sum of monthly-equivalent subscription amounts
        var mrr = 0m;
        foreach (var sub in activeSubscriptions)
        {
            mrr += GetMonthlyEquivalent(sub.ProductId);
        }

        // ARPU: MRR / active subscribers
        var arpu = activeSubscribers > 0
            ? Math.Round(mrr / activeSubscribers, 2)
            : 0m;

        // Churn rate: cancellations in the last 30 days / active subscribers at start of period
        var thirtyDaysAgo = asOfUtc.AddDays(-30);
        var cancellationsInPeriod = allSubscriptions
            .Count(s => s.CancelledAtUtc.HasValue
                        && s.CancelledAtUtc.Value >= thirtyDaysAgo
                        && s.CancelledAtUtc.Value <= asOfUtc);

        // Active at start = current active + those who cancelled in the period
        var activeAtStart = activeSubscribers + cancellationsInPeriod;
        var churnRate = activeAtStart > 0
            ? Math.Round((double)cancellationsInPeriod / activeAtStart, 4)
            : 0.0;

        // LTV: ARPU / churnRate (null if churnRate = 0)
        decimal? estimatedLtv = churnRate > 0
            ? Math.Round(arpu / (decimal)churnRate, 2)
            : null;

        // Revenue trends
        var trends = ComputeRevenueTrends(allSubscriptions, asOfUtc);

        return new RevenueMetrics(
            mrr,
            arpu,
            churnRate,
            estimatedLtv,
            activeSubscribers,
            trialUsers,
            trends);
    }

    internal static decimal GetMonthlyEquivalent(string productId)
    {
        if (ProductPrices.TryGetValue(productId, out var price))
        {
            return price.IsAnnual
                ? Math.Round(price.MonthlyAmount / 12, 2)
                : price.MonthlyAmount;
        }

        return DefaultMonthlyPrice;
    }

    private static FunnelTrends ComputeRevenueTrends(
        IReadOnlyList<Subscription> allSubscriptions,
        DateTimeOffset asOfUtc)
    {
        // Week-over-week: compare new subscriptions this week vs last week
        var thisWeekStart = asOfUtc.AddDays(-7);
        var lastWeekStart = asOfUtc.AddDays(-14);

        var thisWeekNew = CountNewSubscriptions(allSubscriptions, thisWeekStart, asOfUtc);
        var lastWeekNew = CountNewSubscriptions(allSubscriptions, lastWeekStart, thisWeekStart);

        double? wow = lastWeekNew > 0
            ? Math.Round((double)(thisWeekNew - lastWeekNew) / lastWeekNew, 4)
            : null;

        // Month-over-month
        var thisMonthStart = asOfUtc.AddDays(-30);
        var lastMonthStart = asOfUtc.AddDays(-60);

        var thisMonthNew = CountNewSubscriptions(allSubscriptions, thisMonthStart, asOfUtc);
        var lastMonthNew = CountNewSubscriptions(allSubscriptions, lastMonthStart, thisMonthStart);

        double? mom = lastMonthNew > 0
            ? Math.Round((double)(thisMonthNew - lastMonthNew) / lastMonthNew, 4)
            : null;

        return new FunnelTrends(wow, mom);
    }

    private static int CountNewSubscriptions(
        IReadOnlyList<Subscription> subscriptions,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return subscriptions.Count(s =>
            s.CreatedAtUtc >= start && s.CreatedAtUtc < end);
    }
}
