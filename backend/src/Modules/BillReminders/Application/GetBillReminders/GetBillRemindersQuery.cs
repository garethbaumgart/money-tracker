namespace MoneyTracker.Modules.BillReminders.Application.GetBillReminders;

public sealed record GetBillRemindersQuery(Guid HouseholdId, Guid RequestingUserId);
