using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class RetentionCalculator(
    IActivationEventRepository activationEventRepository) : IRetentionDataSource
{
    private static readonly int[] RetentionDays = [1, 7, 14, 30];

    public async Task<IReadOnlyList<CohortRetention>> GetRetentionCohortsAsync(
        int cohortCount,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken)
    {
        var allEvents = await activationEventRepository.GetAllAsync(cancellationToken);

        // Group signup events by ISO week
        var signupEvents = allEvents
            .Where(e => e.Milestone == ActivationMilestone.SignupCompleted)
            .ToArray();

        var cohortGroups = signupEvents
            .GroupBy(e => CohortKey.FromDate(e.OccurredAtUtc).Value)
            .OrderByDescending(g => g.Key)
            .Take(cohortCount)
            .OrderBy(g => g.Key)
            .ToArray();

        // Build a lookup of all activity events (any milestone after signup) per user
        var activityByUser = allEvents
            .GroupBy(e => e.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.OccurredAtUtc).ToArray());

        var cohorts = new List<CohortRetention>();
        foreach (var group in cohortGroups)
        {
            var signups = group.Select(e => e.UserId).Distinct().ToArray();
            var signupCount = signups.Length;

            // Determine the earliest signup date for this cohort
            var earliestSignup = group.Min(e => e.OccurredAtUtc);

            var retentionRates = new double?[RetentionDays.Length];
            for (var i = 0; i < RetentionDays.Length; i++)
            {
                var days = RetentionDays[i];
                var checkDate = earliestSignup.AddDays(days);

                // If the check date hasn't elapsed yet, retention is null (AC-6)
                if (checkDate > asOfUtc)
                {
                    retentionRates[i] = null;
                    continue;
                }

                // Count users who had any activity on or after the check date
                var activeCount = 0;
                foreach (var userId in signups)
                {
                    if (!activityByUser.TryGetValue(userId, out var activities))
                    {
                        continue;
                    }

                    // Find the user's signup date
                    var userSignupDate = group
                        .Where(e => e.UserId == userId)
                        .Min(e => e.OccurredAtUtc);

                    var userCheckDate = userSignupDate.AddDays(days);

                    // User is active if they have any event at or after their check date
                    // that is not the signup event itself
                    var hasActivity = activities.Any(a =>
                        a >= userCheckDate && a > userSignupDate);

                    if (hasActivity)
                    {
                        activeCount++;
                    }
                }

                retentionRates[i] = signupCount > 0
                    ? Math.Round((double)activeCount / signupCount, 4)
                    : 0.0;
            }

            cohorts.Add(new CohortRetention(
                group.Key,
                signupCount,
                retentionRates[0],
                retentionRates[1],
                retentionRates[2],
                retentionRates[3]));
        }

        return cohorts;
    }
}
