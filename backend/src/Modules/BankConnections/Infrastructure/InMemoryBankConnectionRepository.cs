using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class InMemoryBankConnectionRepository : IBankConnectionRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<BankConnection>> _connectionsByHousehold = new();
    private readonly Dictionary<string, BankConnection> _connectionsByConsentSession = new();

    public Task AddAsync(BankConnection connection, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            if (!_connectionsByHousehold.TryGetValue(connection.HouseholdId, out var list))
            {
                list = [];
                _connectionsByHousehold[connection.HouseholdId] = list;
            }

            list.Add(connection);

            if (!string.IsNullOrWhiteSpace(connection.ConsentSessionId))
            {
                _connectionsByConsentSession[connection.ConsentSessionId] = connection;
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(BankConnection connection, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<BankConnection?> GetByConsentSessionIdAsync(
        string consentSessionId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<BankConnection?>(cancellationToken);
        }

        lock (_sync)
        {
            _connectionsByConsentSession.TryGetValue(consentSessionId, out var connection);
            return Task.FromResult(connection);
        }
    }

    public Task<IReadOnlyCollection<BankConnection>> GetByHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BankConnection>>(cancellationToken);
        }

        lock (_sync)
        {
            if (!_connectionsByHousehold.TryGetValue(householdId, out var list))
            {
                return Task.FromResult<IReadOnlyCollection<BankConnection>>(Array.Empty<BankConnection>());
            }

            return Task.FromResult<IReadOnlyCollection<BankConnection>>(list.ToArray());
        }
    }

    public Task<IReadOnlyCollection<BankConnection>> GetActiveConnectionsAsync(
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BankConnection>>(cancellationToken);
        }

        lock (_sync)
        {
            var active = _connectionsByHousehold.Values
                .SelectMany(list => list)
                .Where(c => c.Status == BankConnectionStatus.Active)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<BankConnection>>(active);
        }
    }
}
