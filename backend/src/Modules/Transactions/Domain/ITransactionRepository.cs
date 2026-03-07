namespace MoneyTracker.Modules.Transactions.Domain;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Transaction>> GetByHouseholdAsync(
        Guid householdId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken);
}
