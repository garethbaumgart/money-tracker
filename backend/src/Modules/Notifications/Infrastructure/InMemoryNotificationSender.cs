using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.Notifications.Infrastructure;

public sealed class InMemoryNotificationSender : INotificationSender
{
    public Task<NotificationDispatchResult> SendReminderAsync(
        NotificationMessage message,
        DeviceToken token,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<NotificationDispatchResult>(cancellationToken);
        }

        return Task.FromResult(NotificationDispatchResult.Success());
    }
}
