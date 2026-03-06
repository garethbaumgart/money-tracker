namespace MoneyTracker.Modules.BillReminders.Domain;

public sealed class BillReminderDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
