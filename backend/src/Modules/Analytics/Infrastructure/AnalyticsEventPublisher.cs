using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.SharedKernel.Analytics;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class AnalyticsEventPublisher(
    IActivationEventRepository repository,
    TimeProvider timeProvider,
    ILogger<AnalyticsEventPublisher> logger) : IAnalyticsEventPublisher
{
    public async Task PublishAsync(
        Guid userId,
        string milestone,
        Guid? householdId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ActivationMilestoneExtensions.TryParse(milestone, out var parsedMilestone))
            {
                logger.LogWarning("Invalid activation milestone: {Milestone}", milestone);
                return;
            }

            var exists = await repository.ExistsAsync(userId, parsedMilestone, cancellationToken);
            if (exists)
            {
                return;
            }

            var nowUtc = timeProvider.GetUtcNow();
            var activationEvent = ActivationEvent.Create(
                userId,
                parsedMilestone,
                householdId,
                "backend",
                region: null,
                metadata: null,
                occurredAtUtc: nowUtc,
                recordedAtUtc: nowUtc);

            await repository.AddAsync(activationEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish activation event: {Milestone} for user {UserId}", milestone, userId);
        }
    }
}
