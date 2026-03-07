using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

// PostgreSQL index recommendations:
// - IX_subscriptions_household_id: UNIQUE index on subscriptions(household_id) for household lookups
// - IX_subscriptions_app_user_id: UNIQUE index on subscriptions(revenue_cat_app_user_id) for RevenueCat lookups
// - IX_subscriptions_trial_expiry: index on subscriptions(status, trial_expires_at_utc) WHERE status = 'Trial' for expired trial queries
public sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Subscription> _subscriptionsByHousehold = new();
    private readonly Dictionary<string, Subscription> _subscriptionsByAppUserId = new();

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _subscriptionsByHousehold[subscription.HouseholdId] = subscription;
            _subscriptionsByAppUserId[subscription.RevenueCatAppUserId] = subscription;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        // In-memory: the reference is already updated.
        // Re-index to ensure consistency.
        lock (_sync)
        {
            _subscriptionsByHousehold[subscription.HouseholdId] = subscription;
            _subscriptionsByAppUserId[subscription.RevenueCatAppUserId] = subscription;
        }

        return Task.CompletedTask;
    }

    public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Subscription?>(cancellationToken);
        }

        lock (_sync)
        {
            var subscription = _subscriptionsByHousehold.Values
                .FirstOrDefault(s => s.Id == id);
            return Task.FromResult(subscription);
        }
    }

    public Task<Subscription?> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Subscription?>(cancellationToken);
        }

        lock (_sync)
        {
            _subscriptionsByHousehold.TryGetValue(householdId, out var subscription);
            return Task.FromResult(subscription);
        }
    }

    public Task<Subscription?> GetByRevenueCatAppUserIdAsync(string appUserId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Subscription?>(cancellationToken);
        }

        lock (_sync)
        {
            _subscriptionsByAppUserId.TryGetValue(appUserId, out var subscription);
            return Task.FromResult(subscription);
        }
    }

    public Task<IReadOnlyList<Subscription>> GetExpiredTrialsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<Subscription>>(cancellationToken);
        }

        lock (_sync)
        {
            var expired = _subscriptionsByHousehold.Values
                .Where(s => s.Status == SubscriptionStatus.Trial
                            && s.TrialExpiresAtUtc.HasValue
                            && s.TrialExpiresAtUtc.Value <= asOfUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<Subscription>>(expired);
        }
    }
}
