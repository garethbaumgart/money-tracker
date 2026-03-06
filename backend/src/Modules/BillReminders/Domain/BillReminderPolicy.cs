namespace MoneyTracker.Modules.BillReminders.Domain;

public static class BillReminderPolicy
{
    public static readonly TimeSpan MaxFutureWindow = TimeSpan.FromDays(730);
    public static readonly TimeSpan MaxPastWindow = TimeSpan.FromDays(365);
}
