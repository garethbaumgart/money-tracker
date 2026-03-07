using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;

public sealed class GetActivationFunnelHandler(
    IActivationEventRepository repository,
    TimeProvider timeProvider)
{
    public async Task<GetActivationFunnelResult> HandleAsync(
        GetActivationFunnelQuery query,
        CancellationToken cancellationToken)
    {
        var sinceUtc = timeProvider.GetUtcNow().AddDays(-query.PeriodDays);

        var platform = string.Equals(query.Platform, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Platform;

        var region = string.Equals(query.Region, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : query.Region;

        var events = await repository.GetByPeriodAsync(
            sinceUtc, platform, region, cancellationToken);

        // Build user-milestone lookup: distinct users per milestone
        var usersByMilestone = new Dictionary<ActivationMilestone, HashSet<Guid>>();
        foreach (var milestone in ActivationMilestoneExtensions.OrderedStages)
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

        // Compute total users (users who reached at least the first milestone)
        var firstStage = ActivationMilestoneExtensions.OrderedStages[0];
        var totalUsers = usersByMilestone[firstStage].Count;

        // Build funnel stages
        var stages = new List<FunnelStage>();
        for (var i = 0; i < ActivationMilestoneExtensions.OrderedStages.Count; i++)
        {
            var milestone = ActivationMilestoneExtensions.OrderedStages[i];
            var userCount = usersByMilestone[milestone].Count;

            var conversionRate = totalUsers > 0
                ? (double)userCount / totalUsers
                : 0.0;

            var dropOffRate = 0.0;
            if (i > 0)
            {
                var previousMilestone = ActivationMilestoneExtensions.OrderedStages[i - 1];
                var previousCount = usersByMilestone[previousMilestone].Count;
                dropOffRate = previousCount > 0
                    ? 1.0 - ((double)userCount / previousCount)
                    : 0.0;
            }

            stages.Add(new FunnelStage(
                milestone.ToSnakeCase(),
                userCount,
                Math.Round(conversionRate, 4),
                Math.Round(dropOffRate, 4)));
        }

        // Build cohort summaries grouped by ISO week of signup_completed event
        var signupEvents = events
            .Where(e => e.Milestone == ActivationMilestone.SignupCompleted)
            .ToArray();

        var paidUsers = events
            .Where(e => e.Milestone == ActivationMilestone.PaidConversion)
            .Select(e => e.UserId)
            .ToHashSet();

        var cohortGroups = signupEvents
            .GroupBy(e => CohortKey.FromDate(e.OccurredAtUtc).Value)
            .OrderBy(g => g.Key);

        var cohorts = new List<CohortSummary>();
        foreach (var group in cohortGroups)
        {
            var signupCount = group.Select(e => e.UserId).Distinct().Count();
            var paidCount = group
                .Select(e => e.UserId)
                .Distinct()
                .Count(userId => paidUsers.Contains(userId));

            var paidConversionRate = signupCount > 0
                ? (double)paidCount / signupCount
                : 0.0;

            cohorts.Add(new CohortSummary(
                group.Key,
                signupCount,
                Math.Round(paidConversionRate, 4)));
        }

        return GetActivationFunnelResult.Success(
            query.PeriodDays,
            query.Platform,
            query.Region,
            totalUsers,
            stages,
            cohorts);
    }
}
