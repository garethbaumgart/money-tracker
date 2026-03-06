namespace MoneyTracker.Modules.Notifications.Domain;

public sealed record DeviceToken(
    Guid UserId,
    string DeviceId,
    string Token,
    string Platform,
    DateTimeOffset RegisteredAtUtc);
