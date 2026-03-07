namespace MoneyTracker.Modules.Notifications.Domain;

public static class NotificationErrors
{
    public const string ValidationError = "validation_error";
    public const string NotificationDeviceIdRequired = "notification_device_id_required";
    public const string NotificationTokenRequired = "notification_token_required";
    public const string NotificationPlatformRequired = "notification_platform_required";
    public const string NotificationDispatchFailed = "notification_dispatch_failed";
}
