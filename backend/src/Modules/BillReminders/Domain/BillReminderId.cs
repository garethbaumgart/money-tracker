namespace MoneyTracker.Modules.BillReminders.Domain;

public readonly record struct BillReminderId(Guid Value)
{
    public static BillReminderId New() => new(Guid.NewGuid());
}
