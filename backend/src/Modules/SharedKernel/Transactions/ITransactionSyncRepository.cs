namespace MoneyTracker.Modules.SharedKernel.Transactions;

public interface ITransactionSyncRepository
{
    Task<bool> ExistsByExternalIdAsync(
        Guid bankConnectionId,
        string externalTransactionId,
        CancellationToken cancellationToken);

    Task AddSyncedTransactionAsync(
        SyncedTransaction transaction,
        CancellationToken cancellationToken);

    Task AddSyncedTransactionsAsync(
        IReadOnlyCollection<SyncedTransaction> transactions,
        CancellationToken cancellationToken);
}
