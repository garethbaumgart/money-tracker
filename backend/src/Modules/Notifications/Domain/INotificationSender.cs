namespace MoneyTracker.Modules.Notifications.Domain;

public interface INotificationSender
{
    Task<NotificationDispatchResult> SendReminderAsync(
        NotificationMessage message,
        DeviceToken token,
        CancellationToken cancellationToken);
}
