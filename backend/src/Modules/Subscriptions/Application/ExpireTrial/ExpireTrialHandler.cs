using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.ExpireTrial;

public sealed class ExpireTrialHandler(
    ISubscriptionRepository repository,
    ILogger<ExpireTrialHandler> logger)
{
    public async Task<ExpireTrialResult> HandleAsync(
        ExpireTrialCommand command,
        CancellationToken cancellationToken)
    {
        // AC-3: Query for expired trials and transition them to None
        var expiredTrials = await repository.GetExpiredTrialsAsync(
            command.AsOfUtc,
            cancellationToken);

        var expiredCount = 0;

        foreach (var subscription in expiredTrials)
        {
            subscription.ExpireTrial(command.AsOfUtc);
            await repository.UpdateAsync(subscription, cancellationToken);
            expiredCount++;

            logger.LogInformation(
                "Trial expired for household {HouseholdId}. Trial was due at {TrialExpiresAtUtc}.",
                subscription.HouseholdId,
                subscription.TrialExpiresAtUtc);
        }

        return new ExpireTrialResult(expiredCount);
    }
}

public sealed record ExpireTrialResult(int ExpiredCount);
