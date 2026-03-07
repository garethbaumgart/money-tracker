using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.BillReminders.Infrastructure;

public sealed class BillReminderDataExportParticipant : IUserDataExportParticipant, IUserDeletionParticipant
{
    public Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Bill reminders are household-scoped, not user-scoped.
        return Task.FromResult<object>(new { note = "Bill reminder data is household-scoped." });
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Bill reminders are household-scoped and not deleted with user.
        return Task.CompletedTask;
    }
}
