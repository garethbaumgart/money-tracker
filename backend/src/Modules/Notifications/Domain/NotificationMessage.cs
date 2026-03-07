namespace MoneyTracker.Modules.Notifications.Domain;

public sealed record NotificationMessage(
    Guid ReminderId,
    string Title,
    decimal Amount,
    DateTimeOffset DueDateUtc);
