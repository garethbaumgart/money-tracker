namespace MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;

public sealed record DispatchDueRemindersResult(int Attempted, int Sent, int Failed);
