using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.CheckFeatureAccess;

public sealed class CheckFeatureAccessHandler(
    ISubscriptionEntitlementService entitlementService)
{
    public async Task<CheckFeatureAccessResult> HandleAsync(
        CheckFeatureAccessQuery query,
        CancellationToken cancellationToken)
    {
        var entitlement = await entitlementService.GetEntitlementsAsync(
            query.HouseholdId,
            cancellationToken);

        var entitlementSet = EntitlementSet.ForTier(entitlement.Tier);
        var tierName = entitlement.Tier.ToString();

        if (entitlementSet.HasFeature(query.Feature))
        {
            return CheckFeatureAccessResult.Allowed(tierName);
        }

        return CheckFeatureAccessResult.Denied(tierName);
    }
}
