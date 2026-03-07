using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

/// <summary>
/// In-memory test implementation of IRevenueCatClient.
/// Returns null by default (no active subscription in provider).
/// Can be pre-configured with subscriber info for testing.
/// </summary>
public sealed class InMemoryRevenueCatClient : IRevenueCatClient
{
    private readonly Dictionary<string, SubscriberInfo> _subscribers = new();

    public void SetSubscriber(string appUserId, SubscriberInfo info)
    {
        _subscribers[appUserId] = info;
    }

    public void Clear()
    {
        _subscribers.Clear();
    }

    public Task<SubscriberInfo?> GetSubscriberAsync(
        string appUserId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<SubscriberInfo?>(cancellationToken);
        }

        _subscribers.TryGetValue(appUserId, out var info);
        return Task.FromResult(info);
    }
}
