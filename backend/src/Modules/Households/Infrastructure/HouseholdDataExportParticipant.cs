using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class HouseholdDataExportParticipant : IUserDataExportParticipant, IUserDeletionParticipant
{
    public Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Household membership data is tied to households, not individual user records.
        // Export returns a placeholder indicating membership exists.
        return Task.FromResult<object>(new { note = "Household membership data is part of household records." });
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Household membership removal would be handled during purge.
        // Soft-delete phase: no immediate data removal required.
        return Task.CompletedTask;
    }
}
