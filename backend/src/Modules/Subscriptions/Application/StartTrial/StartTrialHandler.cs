using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.StartTrial;

public sealed class StartTrialHandler(
    ISubscriptionRepository repository,
    TimeProvider timeProvider,
    ILogger<StartTrialHandler> logger)
{
    private const int DefaultTrialDays = 14;
    private const string TrialProductId = "trial_premium";

    public async Task<StartTrialResult> HandleAsync(
        StartTrialCommand command,
        CancellationToken cancellationToken)
    {
        // AC-2: Idempotent — do not create duplicate or reset trial
        var existing = await repository.GetByHouseholdIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Subscription already exists for household {HouseholdId} with status {Status}. Skipping trial grant.",
                command.HouseholdId,
                existing.Status);
            return StartTrialResult.AlreadyExists();
        }

        var nowUtc = timeProvider.GetUtcNow();
        var trialExpiresAtUtc = nowUtc.AddDays(DefaultTrialDays);

        // AC-1: Create subscription with Trial status
        var subscription = Subscription.CreateTrial(
            command.HouseholdId,
            command.HouseholdId.ToString(),
            TrialProductId,
            nowUtc,
            trialExpiresAtUtc,
            nowUtc);

        await repository.AddAsync(subscription, cancellationToken);

        logger.LogInformation(
            "Trial started for household {HouseholdId}. Expires at {TrialExpiresAtUtc}.",
            command.HouseholdId,
            trialExpiresAtUtc);

        return StartTrialResult.Success(trialExpiresAtUtc);
    }
}
