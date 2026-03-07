using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.BillReminders.Infrastructure;

// PostgreSQL index recommendations:
// - IX_bill_reminders_household_id: index on bill_reminders(household_id) for household-scoped queries
// - IX_bill_reminders_due: index on bill_reminders(next_due_date_utc) for due reminder lookups
// - IX_bill_reminder_dispatch_records_key: UNIQUE index on bill_reminder_dispatch_records(reminder_id, due_date_utc) for dispatch deduplication
public sealed class InMemoryBillReminderRepository : IBillReminderRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<BillReminder>> _remindersByHousehold = new();
    private readonly HashSet<(BillReminderId, DateTimeOffset)> _dispatchKeys = new();
    private readonly List<BillReminderDispatchRecord> _dispatchRecords = new();

    public Task AddAsync(BillReminder reminder, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            if (!_remindersByHousehold.TryGetValue(reminder.HouseholdId, out var list))
            {
                list = [];
                _remindersByHousehold[reminder.HouseholdId] = list;
            }

            list.Add(reminder);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<BillReminder>> GetByHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BillReminder>>(cancellationToken);
        }

        lock (_sync)
        {
            if (!_remindersByHousehold.TryGetValue(householdId, out var list))
            {
                return Task.FromResult<IReadOnlyCollection<BillReminder>>(Array.Empty<BillReminder>());
            }

            return Task.FromResult<IReadOnlyCollection<BillReminder>>(list.ToArray());
        }
    }

    public Task<IReadOnlyCollection<BillReminder>> GetDueAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BillReminder>>(cancellationToken);
        }

        lock (_sync)
        {
            var due = _remindersByHousehold.Values
                .SelectMany(reminders => reminders)
                .Where(reminder => reminder.IsDue(nowUtc))
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<BillReminder>>(due);
        }
    }

    public Task UpdateAsync(BillReminder reminder, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasDispatchRecordAsync(
        BillReminderId reminderId,
        DateTimeOffset dueDateUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult(_dispatchKeys.Contains((reminderId, dueDateUtc)));
        }
    }

    public Task RecordDispatchAsync(
        BillReminderDispatchRecord record,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _dispatchRecords.Add(record);
            if (record.Success)
            {
                _dispatchKeys.Add((record.ReminderId, record.DueDateUtc));
            }
            return Task.CompletedTask;
        }
    }
}
