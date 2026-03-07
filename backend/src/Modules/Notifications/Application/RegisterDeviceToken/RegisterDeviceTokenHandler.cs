using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.Notifications.Application.RegisterDeviceToken;

public sealed class RegisterDeviceTokenHandler(
    INotificationTokenRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RegisterDeviceTokenResult> HandleAsync(
        RegisterDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        var deviceId = command.DeviceId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return RegisterDeviceTokenResult.Validation(
                NotificationErrors.NotificationDeviceIdRequired,
                "Device identifier is required.");
        }

        var tokenValue = command.Token?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return RegisterDeviceTokenResult.Validation(
                NotificationErrors.NotificationTokenRequired,
                "Device token is required.");
        }

        var platform = command.Platform?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(platform))
        {
            return RegisterDeviceTokenResult.Validation(
                NotificationErrors.NotificationPlatformRequired,
                "Platform is required.");
        }

        var token = new DeviceToken(
            command.UserId,
            deviceId,
            tokenValue,
            platform,
            timeProvider.GetUtcNow());

        var saved = await repository.UpsertAsync(token, cancellationToken);
        return RegisterDeviceTokenResult.Success(saved);
    }
}
