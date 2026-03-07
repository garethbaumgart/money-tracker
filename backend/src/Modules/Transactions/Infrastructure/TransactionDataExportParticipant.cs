using MoneyTracker.Modules.SharedKernel.Privacy;

namespace MoneyTracker.Modules.Transactions.Infrastructure;

public sealed class TransactionDataExportParticipant : IUserDataExportParticipant, IUserDeletionParticipant
{
    public Task<object> ExportUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Transactions are household-scoped, not user-scoped.
        // User-specific attribution is via CreatedByUserId on the transaction.
        return Task.FromResult<object>(new { note = "Transactions are household-scoped. User attribution is tracked per-transaction." });
    }

    public Task DeleteUserDataAsync(Guid userId, CancellationToken ct)
    {
        // Transactions are household-scoped and not deleted with user.
        // Attribution may be anonymized during purge phase.
        return Task.CompletedTask;
    }
}
