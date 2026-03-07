namespace MoneyTracker.Modules.Transactions.Domain;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
    Task AddRangeAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Transaction>> GetByHouseholdAsync(
        Guid householdId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);
    Task<bool> ExistsByExternalIdAsync(
        Guid bankConnectionId,
        string externalTransactionId,
        CancellationToken cancellationToken);
}
