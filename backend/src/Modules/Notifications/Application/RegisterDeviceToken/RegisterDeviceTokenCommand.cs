namespace MoneyTracker.Modules.Notifications.Application.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(
    string DeviceId,
    string Token,
    string Platform,
    Guid UserId);
