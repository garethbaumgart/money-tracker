namespace MoneyTracker.Modules.BillReminders.Domain;

public sealed class BillReminder
{
    public BillReminderId Id { get; }
    public Guid HouseholdId { get; }
    public Guid CreatedByUserId { get; }
    public string Title { get; }
    public decimal Amount { get; }
    public BillReminderCadence Cadence { get; }
    public DateTimeOffset NextDueDateUtc { get; private set; }
    public DateTimeOffset? LastNotifiedAtUtc { get; private set; }
    public DateTimeOffset? LastNotifiedDueDateUtc { get; private set; }
    public string? LastDispatchErrorCode { get; private set; }
    public string? LastDispatchErrorMessage { get; private set; }
    public int DispatchAttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BillReminder(
        BillReminderId id,
        Guid householdId,
        Guid createdByUserId,
        string title,
        decimal amount,
        BillReminderCadence cadence,
        DateTimeOffset nextDueDateUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        CreatedByUserId = createdByUserId;
        Title = title;
        Amount = amount;
        Cadence = cadence;
        NextDueDateUtc = nextDueDateUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public static BillReminder Create(
        Guid householdId,
        Guid createdByUserId,
        string? title,
        decimal amount,
        DateTimeOffset dueDateUtc,
        BillReminderCadence cadence,
        DateTimeOffset nowUtc)
    {
        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.Length == 0)
        {
            throw new BillReminderDomainException(
                BillReminderErrors.ReminderTitleRequired,
                "Reminder title is required.");
        }

        if (amount <= 0)
        {
            throw new BillReminderDomainException(
                BillReminderErrors.ReminderAmountInvalid,
                "Reminder amount must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(BillReminderCadence), cadence))
        {
            throw new BillReminderDomainException(
                BillReminderErrors.ReminderCadenceInvalid,
                "Reminder cadence is invalid.");
        }

        var normalizedDueDateUtc = dueDateUtc.ToUniversalTime();
        var minDate = nowUtc.Add(-BillReminderPolicy.MaxPastWindow);
        var maxDate = nowUtc.Add(BillReminderPolicy.MaxFutureWindow);
        if (normalizedDueDateUtc < minDate || normalizedDueDateUtc > maxDate)
        {
            throw new BillReminderDomainException(
                BillReminderErrors.ReminderDateOutOfRange,
                "Reminder due date is outside the allowed range.");
        }

        return new BillReminder(
            BillReminderId.New(),
            householdId,
            createdByUserId,
            normalizedTitle,
            amount,
            cadence,
            normalizedDueDateUtc,
            nowUtc);
    }

    public bool IsDue(DateTimeOffset nowUtc)
    {
        if (NextDueDateUtc > nowUtc)
        {
            return false;
        }

        if (NextAttemptAtUtc.HasValue && NextAttemptAtUtc.Value > nowUtc)
        {
            return false;
        }

        return true;
    }

    public bool IsOverdue(DateTimeOffset nowUtc)
    {
        return NextDueDateUtc < nowUtc;
    }

    public void MarkDispatchSuccess(DateTimeOffset nowUtc)
    {
        LastNotifiedAtUtc = nowUtc;
        LastNotifiedDueDateUtc = NextDueDateUtc;
        LastDispatchErrorCode = null;
        LastDispatchErrorMessage = null;
        DispatchAttemptCount = 0;
        NextAttemptAtUtc = null;

        var nextDueDate = CalculateNextDueDate();
        NextDueDateUtc = nextDueDate ?? DateTimeOffset.MaxValue;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkDispatchFailure(
        DateTimeOffset nowUtc,
        TimeSpan retryDelay,
        string? errorCode,
        string? errorMessage)
    {
        DispatchAttemptCount += 1;
        NextAttemptAtUtc = nowUtc.Add(retryDelay);
        LastDispatchErrorCode = errorCode;
        LastDispatchErrorMessage = errorMessage;
        UpdatedAtUtc = nowUtc;
    }

    private DateTimeOffset? CalculateNextDueDate()
    {
        return Cadence switch
        {
            BillReminderCadence.Once => null,
            BillReminderCadence.Weekly => NextDueDateUtc.AddDays(7),
            BillReminderCadence.BiWeekly => NextDueDateUtc.AddDays(14),
            BillReminderCadence.Monthly => NextDueDateUtc.AddMonths(1),
            _ => null
        };
    }

    private static string NormalizeTitle(string? title)
    {
        return title?.Trim() ?? string.Empty;
    }
}
