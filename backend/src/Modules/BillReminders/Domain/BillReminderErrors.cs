namespace MoneyTracker.Modules.BillReminders.Domain;

public static class BillReminderErrors
{
    public const string ValidationError = "validation_error";
    public const string ReminderTitleRequired = "bill_reminder_title_required";
    public const string ReminderAmountInvalid = "bill_reminder_amount_invalid";
    public const string ReminderDateOutOfRange = "bill_reminder_date_out_of_range";
    public const string ReminderCadenceInvalid = "bill_reminder_cadence_invalid";
    public const string ReminderHouseholdNotFound = "bill_reminder_household_not_found";
    public const string ReminderAccessDenied = "bill_reminder_access_denied";
    public const string ReminderDispatchFailed = "bill_reminder_dispatch_failed";
}
