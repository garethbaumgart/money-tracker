using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Infrastructure;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<Transaction>> _transactionsByHousehold = new();

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            if (!_transactionsByHousehold.TryGetValue(transaction.HouseholdId, out var list))
            {
                list = [];
                _transactionsByHousehold[transaction.HouseholdId] = list;
            }

            list.Add(transaction);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Transaction>> GetByHouseholdAsync(
        Guid householdId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<Transaction>>(cancellationToken);
        }

        lock (_sync)
        {
            if (!_transactionsByHousehold.TryGetValue(householdId, out var list))
            {
                return Task.FromResult<IReadOnlyCollection<Transaction>>(Array.Empty<Transaction>());
            }

            IEnumerable<Transaction> query = list;
            if (fromUtc.HasValue)
            {
                var start = fromUtc.Value.ToUniversalTime();
                query = query.Where(transaction => transaction.OccurredAtUtc >= start);
            }

            if (toUtc.HasValue)
            {
                var end = toUtc.Value.ToUniversalTime();
                query = query.Where(transaction => transaction.OccurredAtUtc <= end);
            }

            return Task.FromResult<IReadOnlyCollection<Transaction>>(query.ToArray());
        }
    }
}
