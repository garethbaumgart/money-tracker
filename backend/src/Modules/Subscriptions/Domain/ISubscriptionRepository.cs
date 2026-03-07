namespace MoneyTracker.Modules.Subscriptions.Domain;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken);
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken);
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken);
    Task<Subscription?> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken);
    Task<Subscription?> GetByRevenueCatAppUserIdAsync(string appUserId, CancellationToken cancellationToken);
}
