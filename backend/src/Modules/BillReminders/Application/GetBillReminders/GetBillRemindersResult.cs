using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.GetBillReminders;

public sealed class GetBillRemindersResult
{
    private GetBillRemindersResult(
        IReadOnlyCollection<BillReminder>? reminders,
        string? errorCode,
        string? errorMessage)
    {
        Reminders = reminders;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public IReadOnlyCollection<BillReminder>? Reminders { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Reminders is not null;

    public static GetBillRemindersResult Success(IReadOnlyCollection<BillReminder> reminders)
    {
        return new GetBillRemindersResult(reminders, errorCode: null, errorMessage: null);
    }

    public static GetBillRemindersResult AccessDenied()
    {
        return new GetBillRemindersResult(
            reminders: null,
            BillReminderErrors.ReminderAccessDenied,
            "User is not a member of this household.");
    }

    public static GetBillRemindersResult HouseholdNotFound()
    {
        return new GetBillRemindersResult(
            reminders: null,
            BillReminderErrors.ReminderHouseholdNotFound,
            "Household not found.");
    }
}
