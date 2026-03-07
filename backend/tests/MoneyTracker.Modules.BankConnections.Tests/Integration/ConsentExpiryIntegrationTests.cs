using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;
using MoneyTracker.Modules.BankConnections.Tests.Application;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Transactions;

namespace MoneyTracker.Modules.BankConnections.Tests.Integration;

public sealed class ConsentExpiryIntegrationTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConsentCheck_MixedExpiryDates_CorrectNotifications_NoFalsePositives()
    {
        // P3-3-INT-01: Consent check with mixed expiry dates -> correct notifications, no false positives
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var notificationSender = new InMemoryConsentNotificationSender();

        // Connection 1: Expiring in 5 days -> should get ExpiringSoon notification
        var conn1 = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(5));
        await connectionRepo.AddAsync(conn1, CancellationToken.None);

        // Connection 2: Expired 2 days ago -> should get Expired notification
        var conn2 = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-2));
        await connectionRepo.AddAsync(conn2, CancellationToken.None);

        // Connection 3: Expiring in 45 days -> no action
        var conn3 = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(45));
        await connectionRepo.AddAsync(conn3, CancellationToken.None);

        // Connection 4: Already revoked -> should be skipped
        var conn4 = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(3));
        conn4.MarkConsentRevoked(NowUtc.AddDays(-5));
        await connectionRepo.AddAsync(conn4, CancellationToken.None);

        // Connection 5: No consent expiry set -> should be skipped
        var conn5 = BankConnection.CreatePending(
            householdId, Guid.NewGuid(), "ext-user-5", "session-5", NowUtc.AddDays(-30));
        conn5.Activate($"conn-{conn5.Id.Value:N}", "Bank 5", NowUtc.AddDays(-30).AddMinutes(5));
        await connectionRepo.AddAsync(conn5, CancellationToken.None);

        var handler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        var result = await handler.HandleAsync(
            new CheckConsentExpiryCommand(),
            CancellationToken.None);

        Assert.Equal(1, result.ExpiringSoonCount);
        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(2, result.NotificationsCreated);

        var notifications = notificationSender.GetAll();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.NotificationType == "consent_expiring" && n.ConnectionId == conn1.Id);
        Assert.Contains(notifications, n => n.NotificationType == "consent_expired" && n.ConnectionId == conn2.Id);

        // Verify conn3 was not touched
        Assert.Equal(ConsentStatus.Active, conn3.ConsentStatus);
        Assert.Equal(BankConnectionStatus.Active, conn3.Status);

        // Verify conn4 was skipped (still revoked)
        Assert.Equal(ConsentStatus.Revoked, conn4.ConsentStatus);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullReConsentFlow_Expired_ReConsent_Callback_Active_WithNewExpiry()
    {
        // P3-3-INT-02: Full re-consent flow: expired -> re-consent -> callback -> active with new expiry
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var notificationSender = new InMemoryConsentNotificationSender();

        // Create an active connection that has expired
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-1));
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        // Step 1: Run consent expiry check -> connection becomes Expired
        var checkHandler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        await checkHandler.HandleAsync(new CheckConsentExpiryCommand(), CancellationToken.None);
        Assert.Equal(BankConnectionStatus.Expired, connection.Status);
        Assert.Equal(ConsentStatus.Expired, connection.ConsentStatus);

        // Step 2: Simulate re-consent by updating the consent session ID
        // (In real flow, ReConsentHandler calls CreateConsentSession and updates the connection)
        var newSessionId = "re-consent-session-new";
        connection.UpdateConsentSessionId(newSessionId, NowUtc);
        await connectionRepo.UpdateAsync(connection, CancellationToken.None);

        // Step 3: Process re-consent callback
        var providerAdapter = new SyncStubBankProviderAdapter([]);
        var callbackHandler = new ProcessCallbackHandler(
            connectionRepo,
            providerAdapter,
            new NoopAnalyticsEventPublisher(),
            new StubTimeProvider(NowUtc));

        var callbackResult = await callbackHandler.HandleAsync(
            new ProcessCallbackCommand(newSessionId),
            CancellationToken.None);

        Assert.True(callbackResult.IsSuccess);
        Assert.NotNull(callbackResult.Connection);
        Assert.Equal(BankConnectionStatus.Active, callbackResult.Connection.Status);
        Assert.Equal(ConsentStatus.Active, callbackResult.Connection.ConsentStatus);
        Assert.NotNull(callbackResult.Connection.ConsentExpiresAtUtc);
        // New expiry should be ~90 days from now
        Assert.True(callbackResult.Connection.ConsentExpiresAtUtc > NowUtc.AddDays(80));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncJob_MixOfActiveAndExpired_OnlyActiveSynced()
    {
        // P3-3-INT-03: Sync job with mix of Active and Expired connections -> only Active synced
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();

        // Connection 1: Active with valid consent
        var activeConn = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(30));
        await connectionRepo.AddAsync(activeConn, CancellationToken.None);

        // Connection 2: Expired consent
        var expiredConn = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-5));
        expiredConn.MarkConsentExpired(NowUtc.AddDays(-1));
        await connectionRepo.AddAsync(expiredConn, CancellationToken.None);

        // Connection 3: Revoked consent
        var revokedConn = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(10));
        revokedConn.MarkConsentRevoked(NowUtc.AddDays(-1));
        await connectionRepo.AddAsync(revokedConn, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        var providerAdapter = new SyncStubBankProviderAdapter(
        [
            new ProviderTransaction("txn-001", 10m, NowUtc.AddHours(-1), "Test Transaction")
        ]);

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new NoOpSyncEventRepository(),
            new NoopAnalyticsEventPublisher(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        var result = await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Only the active connection should have been synced
        Assert.Equal(1, result.SyncedCount);
        Assert.Equal(0, result.FailedConnections);
    }

    private static BankConnection CreateActiveConnectionWithExpiry(Guid householdId, DateTimeOffset consentExpiresAtUtc)
    {
        var connection = BankConnection.CreatePending(
            householdId, Guid.NewGuid(), $"ext-user-{Guid.NewGuid():N}", $"session-{Guid.NewGuid():N}", NowUtc.AddDays(-30));
        connection.Activate($"conn-{connection.Id.Value:N}", "Test Bank", NowUtc.AddDays(-30).AddMinutes(5));
        connection.UpdateConsentExpiry(consentExpiresAtUtc, NowUtc.AddDays(-30).AddMinutes(5));
        return connection;
    }
}

internal sealed class NoOpSyncEventRepository : ISyncEventRepository
{
    public Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IReadOnlyCollection<SyncEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<SyncEvent>>(Array.Empty<SyncEvent>());
}
