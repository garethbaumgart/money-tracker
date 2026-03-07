using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class FunnelDataAggregator(
    IActivationEventRepository activationEventRepository,
    TimeProvider timeProvider) : IFunnelDataSource
{
    // The 7 funnel stages from the issue specification, mapped to ActivationMilestone values.
    internal static readonly (string Name, ActivationMilestone Milestone)[] FunnelStages =
    [
        ("signup", ActivationMilestone.SignupCompleted),
        ("onboarding_complete", ActivationMilestone.HouseholdCreated),
        ("first_transaction", ActivationMilestone.FirstTransactionCreated),
        ("bank_link", ActivationMilestone.BankLinkCompleted),
        ("paywall_view", ActivationMilestone.PaywallViewed),
        ("trial_start", ActivationMilestone.TrialStarted),
        ("paid_conversion", ActivationMilestone.PaidConversion),
    ];

    public async Task<FunnelReport> GetFunnelReportAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var currentPeriodEvents = await GetEventsInPeriodAsync(periodStart, periodEnd, cancellationToken);
        var stages = ComputeStages(currentPeriodEvents);
        var topDropOffs = ComputeTopDropOffs(stages);
        var overallConversion = ComputeOverallConversion(stages);
        var trends = await ComputeTrendsAsync(periodStart, periodEnd, cancellationToken);

        return new FunnelReport(
            periodStart,
            periodEnd,
            stages,
            overallConversion,
            topDropOffs,
            trends);
    }

    private async Task<IReadOnlyCollection<ActivationEvent>> GetEventsInPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var allEvents = await activationEventRepository.GetByPeriodAsync(
            periodStart, platform: null, region: null, cancellationToken);

        return allEvents.Where(e => e.OccurredAtUtc <= periodEnd).ToArray();
    }

    internal static IReadOnlyList<FunnelReportStage> ComputeStages(
        IReadOnlyCollection<ActivationEvent> events)
    {
        var usersByMilestone = new Dictionary<ActivationMilestone, HashSet<Guid>>();
        foreach (var (_, milestone) in FunnelStages)
        {
            usersByMilestone[milestone] = [];
        }

        foreach (var evt in events)
        {
            if (usersByMilestone.TryGetValue(evt.Milestone, out var userSet))
            {
                userSet.Add(evt.UserId);
            }
        }

        var stages = new List<FunnelReportStage>();
        for (var i = 0; i < FunnelStages.Length; i++)
        {
            var (name, milestone) = FunnelStages[i];
            var count = usersByMilestone[milestone].Count;

            double conversionRate;
            double dropOffRate;

            if (i == 0)
            {
                conversionRate = 1.0;
                dropOffRate = 0.0;
            }
            else
            {
                var previousMilestone = FunnelStages[i - 1].Milestone;
                var previousCount = usersByMilestone[previousMilestone].Count;

                conversionRate = previousCount > 0
                    ? (double)count / previousCount
                    : 0.0;

                dropOffRate = previousCount > 0
                    ? 1.0 - conversionRate
                    : 0.0;
            }

            stages.Add(new FunnelReportStage(
                name,
                count,
                Math.Round(conversionRate, 4),
                Math.Round(dropOffRate, 4)));
        }

        return stages;
    }

    internal static IReadOnlyList<DropOffAnalysis> ComputeTopDropOffs(
        IReadOnlyList<FunnelReportStage> stages)
    {
        var dropOffs = new List<DropOffAnalysis>();
        for (var i = 1; i < stages.Count; i++)
        {
            var fromStage = stages[i - 1];
            var toStage = stages[i];
            var lostUsers = fromStage.Count - toStage.Count;

            if (lostUsers > 0)
            {
                dropOffs.Add(new DropOffAnalysis(
                    fromStage.Name,
                    toStage.Name,
                    toStage.DropOffRate,
                    lostUsers));
            }
        }

        return dropOffs
            .OrderByDescending(d => d.LostUsers)
            .Take(3)
            .ToArray();
    }

    internal static double ComputeOverallConversion(IReadOnlyList<FunnelReportStage> stages)
    {
        if (stages.Count < 2)
        {
            return 0.0;
        }

        var firstCount = stages[0].Count;
        var lastCount = stages[^1].Count;

        return firstCount > 0
            ? Math.Round((double)lastCount / firstCount, 4)
            : 0.0;
    }

    private async Task<FunnelTrends> ComputeTrendsAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        var periodLength = periodEnd - periodStart;
        var nowUtc = timeProvider.GetUtcNow();

        // Week-over-week: compare this week's signups to last week's signups
        var thisWeekSignups = await CountSignupsInPeriodAsync(
            periodStart, periodEnd, cancellationToken);

        var lastWeekStart = periodStart.AddDays(-7);
        var lastWeekEnd = periodStart;
        var lastWeekSignups = await CountSignupsInPeriodAsync(
            lastWeekStart, lastWeekEnd, cancellationToken);

        double? wow = lastWeekSignups > 0
            ? Math.Round((double)(thisWeekSignups - lastWeekSignups) / lastWeekSignups, 4)
            : null;

        // Month-over-month: compare this period's signups to the equivalent period one month ago
        var lastMonthStart = periodStart.AddDays(-30);
        var lastMonthEnd = periodEnd.AddDays(-30);
        var lastMonthSignups = await CountSignupsInPeriodAsync(
            lastMonthStart, lastMonthEnd, cancellationToken);

        double? mom = lastMonthSignups > 0
            ? Math.Round((double)(thisWeekSignups - lastMonthSignups) / lastMonthSignups, 4)
            : null;

        return new FunnelTrends(wow, mom);
    }

    private async Task<int> CountSignupsInPeriodAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        var events = await activationEventRepository.GetByPeriodAsync(
            start, platform: null, region: null, cancellationToken);

        return events
            .Where(e => e.Milestone == ActivationMilestone.SignupCompleted
                        && e.OccurredAtUtc <= end)
            .Select(e => e.UserId)
            .Distinct()
            .Count();
    }
}
