namespace MoneyTracker.Modules.Subscriptions.Domain;

/// <summary>
/// Subscriber information returned from the payment provider.
/// Provider-agnostic contract for anti-corruption layer.
/// </summary>
public sealed record SubscriberInfo(
    SubscriptionStatus Status,
    string ProductId,
    DateTimeOffset? PeriodStartUtc,
    DateTimeOffset? PeriodEndUtc);

/// <summary>
/// Anti-corruption interface for RevenueCat REST API.
/// Abstracts the provider-specific subscriber lookup.
/// </summary>
public interface IRevenueCatClient
{
    Task<SubscriberInfo?> GetSubscriberAsync(
        string appUserId,
        CancellationToken cancellationToken);
}
