using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.CreateBillReminder;

public sealed record CreateBillReminderCommand(
    Guid HouseholdId,
    string Title,
    decimal Amount,
    DateTimeOffset DueDateUtc,
    BillReminderCadence Cadence,
    Guid RequestingUserId);
