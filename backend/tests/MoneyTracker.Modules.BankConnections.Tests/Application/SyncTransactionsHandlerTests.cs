using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.SharedKernel.Transactions;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class SyncTransactionsHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Dedupe_WithExistingExternalId_TransactionSkipped()
    {
        // P3-2-UNIT-01: Dedupe with existing externalId -> transaction skipped
        var householdId = Guid.NewGuid();
        var externalTxnId = "existing-txn-123";

        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnection(householdId);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        // Pre-populate with an existing synced transaction matching the external ID
        await transactionRepo.AddSyncedTransactionAsync(
            new SyncedTransaction(
                householdId, connection.Id.Value, externalTxnId, 50m,
                NowUtc.AddHours(-1), "Existing", NowUtc),
            CancellationToken.None);

        var providerAdapter = new SyncStubBankProviderAdapter(
        [
            new ProviderTransaction(externalTxnId, 50m, NowUtc.AddHours(-1), "Existing")
        ]);

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubSyncEventRepository(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        var result = await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.SyncedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Dedupe_WithNewExternalId_TransactionPersisted()
    {
        // P3-2-UNIT-02: Dedupe with new externalId -> transaction persisted
        var householdId = Guid.NewGuid();

        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnection(householdId);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();

        var providerAdapter = new SyncStubBankProviderAdapter(
        [
            new ProviderTransaction("new-txn-456", 75m, NowUtc.AddHours(-1), "New Transaction")
        ]);

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubSyncEventRepository(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        var result = await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SyncedCount);
        Assert.Equal(0, result.SkippedCount);

        // Verify transaction was persisted
        Assert.Equal(1, transactionRepo.GetSyncedCount(householdId));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SyncSuccess_UpdatesSyncState()
    {
        // P3-2-UNIT-03: Sync success updates SyncState (LastSuccessUtc set, ConsecutiveFailures=0)
        var householdId = Guid.NewGuid();

        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnection(householdId);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        var providerAdapter = new SyncStubBankProviderAdapter([]);

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubSyncEventRepository(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.NotNull(connection.SyncState.LastSuccessUtc);
        Assert.Equal(NowUtc, connection.SyncState.LastSuccessUtc);
        Assert.Equal(0, connection.SyncState.ConsecutiveFailures);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SyncFailure_UpdatesSyncState()
    {
        // P3-2-UNIT-04: Sync failure updates SyncState (LastFailureUtc set, ConsecutiveFailures incremented)
        var householdId = Guid.NewGuid();

        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnection(householdId);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        // Provider returns failure
        var providerAdapter = new SyncFailingBankProviderAdapter();

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubSyncEventRepository(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        var result = await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.True(result.IsSuccess); // Handler returns success even with per-connection failures
        Assert.Equal(1, result.FailedConnections);
        Assert.NotNull(connection.SyncState.LastFailureUtc);
        Assert.Equal(NowUtc, connection.SyncState.LastFailureUtc);
        Assert.Equal(1, connection.SyncState.ConsecutiveFailures);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Transaction_WithSourceSynced_FieldsPopulatedCorrectly()
    {
        // P3-2-UNIT-05: Transaction with Source=Synced and ExternalId -> fields populated correctly
        var householdId = Guid.NewGuid();
        var bankConnectionId = Guid.NewGuid();
        var externalTxnId = "ext-txn-789";

        var transaction = Transaction.CreateSynced(
            householdId, bankConnectionId, externalTxnId, 100m,
            NowUtc.AddHours(-2), "Test Synced Transaction", NowUtc);

        Assert.Equal(TransactionSource.Synced, transaction.Source);
        Assert.Equal(externalTxnId, transaction.ExternalTransactionId);
        Assert.Equal(bankConnectionId, transaction.BankConnectionId);
        Assert.Equal(householdId, transaction.HouseholdId);
        Assert.Equal(100m, transaction.Amount);
        Assert.Equal("Test Synced Transaction", transaction.Description);
    }

    private static BankConnection CreateActiveConnection(Guid householdId)
    {
        var connection = BankConnection.CreatePending(
            householdId, Guid.NewGuid(), "ext-user-1", "session-1", NowUtc.AddDays(-1));
        connection.Activate($"conn-{connection.Id.Value:N}", "Test Bank", NowUtc.AddDays(-1).AddMinutes(5));
        return connection;
    }
}

internal sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class StubBankConnectionRepository : IBankConnectionRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<BankConnection>> _connectionsByHousehold = new();
    private readonly Dictionary<string, BankConnection> _connectionsByConsentSession = new();

    public Task AddAsync(BankConnection connection, CancellationToken cancellationToken)
    {
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

    public Task UpdateAsync(BankConnection connection, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<BankConnection?> GetByConsentSessionIdAsync(string consentSessionId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _connectionsByConsentSession.TryGetValue(consentSessionId, out var connection);
            return Task.FromResult(connection);
        }
    }

    public Task<IReadOnlyCollection<BankConnection>> GetByHouseholdAsync(Guid householdId, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (!_connectionsByHousehold.TryGetValue(householdId, out var list))
            {
                return Task.FromResult<IReadOnlyCollection<BankConnection>>(Array.Empty<BankConnection>());
            }
            return Task.FromResult<IReadOnlyCollection<BankConnection>>(list.ToArray());
        }
    }

    public Task<IReadOnlyCollection<BankConnection>> GetActiveConnectionsAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var active = _connectionsByHousehold.Values
                .SelectMany(l => l)
                .Where(c => c.Status == BankConnectionStatus.Active)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<BankConnection>>(active);
        }
    }
}

internal sealed class StubTransactionSyncRepository : ITransactionSyncRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, List<SyncedTransaction>> _transactionsByHousehold = new();

    public Task<bool> ExistsByExternalIdAsync(Guid bankConnectionId, string externalTransactionId, CancellationToken cancellationToken)
    {
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

    public Task AddSyncedTransactionAsync(SyncedTransaction transaction, CancellationToken cancellationToken)
    {
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

    public Task AddSyncedTransactionsAsync(IReadOnlyCollection<SyncedTransaction> transactions, CancellationToken cancellationToken)
    {
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

    public int GetSyncedCount(Guid householdId)
    {
        lock (_sync)
        {
            if (!_transactionsByHousehold.TryGetValue(householdId, out var list))
            {
                return 0;
            }
            return list.Count;
        }
    }
}

internal sealed class SyncStubBankProviderAdapter(IReadOnlyCollection<ProviderTransaction> transactions) : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(new CreateUserResult(true, $"user-{userId:N}", null, null));

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(string externalUserId, CancellationToken cancellationToken)
        => Task.FromResult(new CreateConsentSessionResult(true, "session-1", "https://consent.example.com/session-1", null, null));

    public Task<GetConnectionResult> GetConnectionAsync(string externalUserId, string consentSessionId, CancellationToken cancellationToken)
        => Task.FromResult(new GetConnectionResult(true, "conn-1", "Test Bank", "active", null, null));

    public Task<GetAccountsResult> GetAccountsAsync(string externalConnectionId, CancellationToken cancellationToken)
        => Task.FromResult(new GetAccountsResult(true, Array.Empty<BankAccountInfo>(), null, null));

    public Task<GetTransactionsResult> GetTransactionsAsync(string externalConnectionId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
        => Task.FromResult(new GetTransactionsResult(true, transactions, null, null));
}

internal sealed class SyncFailingBankProviderAdapter : IBankProviderAdapter
{
    public Task<CreateUserResult> CreateUserAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(new CreateUserResult(true, $"user-{userId:N}", null, null));

    public Task<CreateConsentSessionResult> CreateConsentSessionAsync(string externalUserId, CancellationToken cancellationToken)
        => Task.FromResult(new CreateConsentSessionResult(true, "session-1", "https://consent.example.com/session-1", null, null));

    public Task<GetConnectionResult> GetConnectionAsync(string externalUserId, string consentSessionId, CancellationToken cancellationToken)
        => Task.FromResult(new GetConnectionResult(true, "conn-1", "Test Bank", "active", null, null));

    public Task<GetAccountsResult> GetAccountsAsync(string externalConnectionId, CancellationToken cancellationToken)
        => Task.FromResult(new GetAccountsResult(true, Array.Empty<BankAccountInfo>(), null, null));

    public Task<GetTransactionsResult> GetTransactionsAsync(string externalConnectionId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
        => Task.FromResult(new GetTransactionsResult(false, null, BankConnectionErrors.SyncProviderError, "Provider error"));
}

internal sealed class StubSyncEventRepository : ISyncEventRepository
{
    private readonly object _sync = new();
    private readonly List<SyncEvent> _events = [];

    public Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken)
    {
        lock (_sync) { _events.Add(syncEvent); }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(
                _events.Where(e => e.OccurredAtUtc >= since).ToArray());
        }
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByRegionAsync(string region, DateTimeOffset since, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(
                _events.Where(e => e.OccurredAtUtc >= since && e.Region.Equals(region, StringComparison.OrdinalIgnoreCase)).ToArray());
        }
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByInstitutionAsync(string institution, DateTimeOffset since, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(
                _events.Where(e => e.OccurredAtUtc >= since && e.Institution.Equals(institution, StringComparison.OrdinalIgnoreCase)).ToArray());
        }
    }

    public IReadOnlyCollection<SyncEvent> GetAll()
    {
        lock (_sync) { return _events.ToArray(); }
    }
}

internal sealed class StubLinkEventRepository : ILinkEventRepository
{
    private readonly object _sync = new();
    private readonly List<LinkEvent> _events = [];

    public Task AddAsync(LinkEvent linkEvent, CancellationToken cancellationToken)
    {
        lock (_sync) { _events.Add(linkEvent); }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<LinkEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<LinkEvent>>(
                _events.Where(e => e.OccurredAtUtc >= since).ToArray());
        }
    }

    public IReadOnlyCollection<LinkEvent> GetAll()
    {
        lock (_sync) { return _events.ToArray(); }
    }
}
