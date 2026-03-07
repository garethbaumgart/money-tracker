using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Budgets.Infrastructure;

public sealed class BudgetDataExportParticipant : IUserDataExportParticipant, IUserDeletionParticipant
{
    public Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Budgets are household-scoped, not user-scoped.
        return Task.FromResult<object>(new { note = "Budget data is household-scoped." });
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Budgets are household-scoped and not deleted with user.
        return Task.CompletedTask;
    }
}
