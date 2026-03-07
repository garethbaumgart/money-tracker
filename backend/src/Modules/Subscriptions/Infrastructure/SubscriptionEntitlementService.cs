using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

public sealed class SubscriptionEntitlementService(
    ISubscriptionRepository repository)
    : Domain.ISubscriptionEntitlementService,
      SharedKernel.Subscriptions.ISubscriptionEntitlementService
{
    public async Task<EntitlementResult> GetEntitlementsAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByHouseholdIdAsync(
            householdId,
            cancellationToken);

        if (subscription is null)
        {
            var freeSet = EntitlementSet.ForTier(SubscriptionTier.Free);
            return new EntitlementResult(
                SubscriptionTier.Free,
                freeSet.FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null);
        }

        var tier = MapStatusToTier(subscription.Status);
        var entitlementSet = EntitlementSet.ForTier(tier);

        DateTimeOffset? trialExpiresAtUtc = tier == SubscriptionTier.Trial
            ? subscription.CurrentPeriodEndUtc
            : null;

        return new EntitlementResult(
            tier,
            entitlementSet.FeatureKeys,
            trialExpiresAtUtc,
            subscription.CurrentPeriodEndUtc);
    }

    public async Task<bool> IsFeatureAllowedAsync(
        Guid householdId,
        string featureKey,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FeatureKey>(featureKey, ignoreCase: true, out var feature))
        {
            return false;
        }

        var entitlement = await GetEntitlementsAsync(householdId, cancellationToken);
        var entitlementSet = EntitlementSet.ForTier(entitlement.Tier);
        return entitlementSet.HasFeature(feature);
    }

    private static SubscriptionTier MapStatusToTier(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Active => SubscriptionTier.Premium,
        SubscriptionStatus.Trial => SubscriptionTier.Trial,
        SubscriptionStatus.BillingIssue => SubscriptionTier.Premium,
        SubscriptionStatus.Cancelled => SubscriptionTier.Premium,
        _ => SubscriptionTier.Free
    };
}
