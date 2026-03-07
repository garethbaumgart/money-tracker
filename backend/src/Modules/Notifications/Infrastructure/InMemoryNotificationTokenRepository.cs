using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.Notifications.Infrastructure;

// PostgreSQL index recommendations:
// - IX_device_tokens_user_device: UNIQUE index on device_tokens(user_id, device_id) for upsert
// - IX_device_tokens_user_id: index on device_tokens(user_id) for multi-user token lookups
public sealed class InMemoryNotificationTokenRepository : INotificationTokenRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<(Guid UserId, string DeviceId), DeviceToken> _tokensByDevice = new();

    public Task<DeviceToken> UpsertAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DeviceToken>(cancellationToken);
        }

        lock (_sync)
        {
            _tokensByDevice[(token.UserId, token.DeviceId)] = token;
            return Task.FromResult(token);
        }
    }

    public Task<IReadOnlyCollection<DeviceToken>> GetTokensForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<DeviceToken>>(cancellationToken);
        }

        lock (_sync)
        {
            var results = _tokensByDevice.Values
                .Where(token => userIds.Contains(token.UserId))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<DeviceToken>>(results);
        }
    }
}
