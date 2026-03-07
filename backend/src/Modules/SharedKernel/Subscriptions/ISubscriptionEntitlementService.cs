namespace MoneyTracker.Modules.SharedKernel.Subscriptions;

/// <summary>
/// Anti-corruption interface for cross-module entitlement checks.
/// Other modules depend on this interface to gate premium features
/// without taking a direct dependency on the Subscriptions module.
/// </summary>
public interface ISubscriptionEntitlementService
{
    Task<bool> IsFeatureAllowedAsync(
        Guid householdId,
        string featureKey,
        CancellationToken cancellationToken);
}
