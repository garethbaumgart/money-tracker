namespace MoneyTracker.Modules.Notifications.Domain;

public interface INotificationTokenRepository
{
    Task<DeviceToken> UpsertAsync(DeviceToken token, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DeviceToken>> GetTokensForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
