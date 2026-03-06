using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.BillReminders.Application.CreateBillReminder;

public sealed class CreateBillReminderResult
{
    private CreateBillReminderResult(BillReminder? reminder, string? errorCode, string? errorMessage)
    {
        Reminder = reminder;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public BillReminder? Reminder { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Reminder is not null;

    public static CreateBillReminderResult Success(BillReminder reminder)
    {
        return new CreateBillReminderResult(reminder, errorCode: null, errorMessage: null);
    }

    public static CreateBillReminderResult Validation(string code, string message)
    {
        return new CreateBillReminderResult(reminder: null, code, message);
    }

    public static CreateBillReminderResult AccessDenied()
    {
        return new CreateBillReminderResult(
            reminder: null,
            BillReminderErrors.ReminderAccessDenied,
            "User is not a member of this household.");
    }

    public static CreateBillReminderResult HouseholdNotFound()
    {
        return new CreateBillReminderResult(
            reminder: null,
            BillReminderErrors.ReminderHouseholdNotFound,
            "Household not found.");
    }
}
