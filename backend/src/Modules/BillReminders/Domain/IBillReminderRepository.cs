namespace MoneyTracker.Modules.BillReminders.Domain;

public interface IBillReminderRepository
{
    Task AddAsync(BillReminder reminder, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BillReminder>> GetByHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BillReminder>> GetDueAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
    Task UpdateAsync(BillReminder reminder, CancellationToken cancellationToken);
    Task<bool> HasDispatchRecordAsync(
        BillReminderId reminderId,
        DateTimeOffset dueDateUtc,
        CancellationToken cancellationToken);
    Task RecordDispatchAsync(
        BillReminderDispatchRecord record,
        CancellationToken cancellationToken);
}
