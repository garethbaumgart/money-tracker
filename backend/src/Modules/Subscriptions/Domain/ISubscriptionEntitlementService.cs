namespace MoneyTracker.Modules.Subscriptions.Domain;

public sealed record EntitlementResult(
    SubscriptionTier Tier,
    IReadOnlySet<FeatureKey> FeatureKeys,
    DateTimeOffset? TrialExpiresAtUtc,
    DateTimeOffset? CurrentPeriodEndUtc);

public interface ISubscriptionEntitlementService
{
    Task<EntitlementResult> GetEntitlementsAsync(
        Guid householdId,
        CancellationToken cancellationToken);
}
