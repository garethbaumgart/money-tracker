namespace MoneyTracker.Modules.BillReminders.Domain;

public sealed record BillReminderDispatchRecord(
    BillReminderId ReminderId,
    DateTimeOffset DueDateUtc,
    DateTimeOffset SentAtUtc,
    bool Success,
    string? ErrorCode,
    string? ErrorMessage);
