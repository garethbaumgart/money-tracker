using MoneyTracker.Modules.SharedKernel.Transactions;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Infrastructure;

public sealed class InMemoryTransactionRepository : ITransactionRepository, ITransactionSyncRepository
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

    public Task AddRangeAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            foreach (var transaction in transactions)
            {
                if (!_transactionsByHousehold.TryGetValue(transaction.HouseholdId, out var list))
                {
                    list = [];
                    _transactionsByHousehold[transaction.HouseholdId] = list;
                }

                list.Add(transaction);
            }
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

    public Task<bool> ExistsByExternalIdAsync(
        Guid bankConnectionId,
        string externalTransactionId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            foreach (var list in _transactionsByHousehold.Values)
            {
                foreach (var transaction in list)
                {
                    if (transaction.BankConnectionId == bankConnectionId
                        && transaction.ExternalTransactionId == externalTransactionId)
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }
    }

    public Task AddSyncedTransactionAsync(
        SyncedTransaction syncedTransaction,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var transaction = Transaction.CreateSynced(
            syncedTransaction.HouseholdId,
            syncedTransaction.BankConnectionId,
            syncedTransaction.ExternalTransactionId,
            syncedTransaction.Amount,
            syncedTransaction.OccurredAtUtc,
            syncedTransaction.Description,
            syncedTransaction.CreatedAtUtc);

        return AddAsync(transaction, cancellationToken);
    }

    public Task AddSyncedTransactionsAsync(
        IReadOnlyCollection<SyncedTransaction> syncedTransactions,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var transactions = syncedTransactions
            .Select(st => Transaction.CreateSynced(
                st.HouseholdId,
                st.BankConnectionId,
                st.ExternalTransactionId,
                st.Amount,
                st.OccurredAtUtc,
                st.Description,
                st.CreatedAtUtc))
            .ToArray();

        return AddRangeAsync(transactions, cancellationToken);
    }
}
