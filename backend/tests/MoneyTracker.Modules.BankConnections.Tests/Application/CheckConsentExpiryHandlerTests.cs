using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;
using MoneyTracker.Modules.BankConnections.Application.ProcessCallback;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Transactions;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class CheckConsentExpiryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConnectionExpiringIn5Days_NotificationCreatedWithTypeConsentExpiring()
    {
        // P3-3-UNIT-01: Connection expiring in 5 days -> notification created with type consent_expiring
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(5));
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var notificationSender = new InMemoryConsentNotificationSender();

        var handler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        var result = await handler.HandleAsync(
            new CheckConsentExpiryCommand(),
            CancellationToken.None);

        Assert.Equal(1, result.ExpiringSoonCount);
        Assert.Equal(0, result.ExpiredCount);
        Assert.Equal(1, result.NotificationsCreated);

        var notifications = notificationSender.GetAll();
        Assert.Single(notifications);
        Assert.Equal("consent_expiring", notifications.First().NotificationType);
        Assert.Equal(connection.Id, notifications.First().ConnectionId);
        Assert.Equal(ConsentStatus.ExpiringSoon, connection.ConsentStatus);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConnectionExpired1DayAgo_StatusTransitionsToExpired_NotificationCreated()
    {
        // P3-3-UNIT-02: Connection expired 1 day ago -> status transitions to Expired, notification created
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-1));
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var notificationSender = new InMemoryConsentNotificationSender();

        var handler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        var result = await handler.HandleAsync(
            new CheckConsentExpiryCommand(),
            CancellationToken.None);

        Assert.Equal(0, result.ExpiringSoonCount);
        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(1, result.NotificationsCreated);

        var notifications = notificationSender.GetAll();
        Assert.Single(notifications);
        Assert.Equal("consent_expired", notifications.First().NotificationType);
        Assert.Equal(BankConnectionStatus.Expired, connection.Status);
        Assert.Equal(ConsentStatus.Expired, connection.ConsentStatus);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConnectionExpiringIn30Days_NoNotification_StatusUnchanged()
    {
        // P3-3-UNIT-03: Connection expiring in 30 days -> no notification, status unchanged
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(30));
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var notificationSender = new InMemoryConsentNotificationSender();

        var handler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        var result = await handler.HandleAsync(
            new CheckConsentExpiryCommand(),
            CancellationToken.None);

        Assert.Equal(0, result.ExpiringSoonCount);
        Assert.Equal(0, result.ExpiredCount);
        Assert.Equal(0, result.NotificationsCreated);
        Assert.Empty(notificationSender.GetAll());
        Assert.Equal(ConsentStatus.Active, connection.ConsentStatus);
        Assert.Equal(BankConnectionStatus.Active, connection.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SyncOrchestratorEncountersExpiredConnection_ConnectionSkipped()
    {
        // P3-3-UNIT-04: Sync orchestrator encounters Expired connection -> connection skipped
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-1));
        // Manually expire the connection
        connection.MarkConsentExpired(NowUtc);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        var providerAdapter = new SyncStubBankProviderAdapter(
        [
            new ProviderTransaction("txn-should-not-sync", 50m, NowUtc.AddHours(-1), "Should Not Sync")
        ]);

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        // Even though we get by household (which includes all connections), the handler should
        // skip expired ones
        var result = await handler.HandleAsync(
            new SyncTransactionsCommand(householdId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.SyncedCount);
        Assert.Equal(0, result.FailedConnections);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReConsentCallbackForExpiredConnection_StatusActive_ExpiryUpdated()
    {
        // P3-3-UNIT-05: Re-consent callback for Expired connection -> status Active, expiry updated
        var householdId = Guid.NewGuid();
        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-1));
        connection.MarkConsentExpired(NowUtc.AddDays(-1));
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        // Re-consent: update consent session ID so callback can find the connection
        var newSessionId = "re-consent-session-123";
        connection.UpdateConsentSessionId(newSessionId, NowUtc);
        await connectionRepo.UpdateAsync(connection, CancellationToken.None);

        var providerAdapter = new SyncStubBankProviderAdapter([]);

        var callbackHandler = new ProcessCallbackHandler(
            connectionRepo,
            providerAdapter,
            new StubTimeProvider(NowUtc));

        var result = await callbackHandler.HandleAsync(
            new ProcessCallbackCommand(newSessionId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Connection);
        Assert.Equal(BankConnectionStatus.Active, result.Connection.Status);
        Assert.Equal(ConsentStatus.Active, result.Connection.ConsentStatus);
        Assert.NotNull(result.Connection.ConsentExpiresAtUtc);
    }

    private static BankConnection CreateActiveConnectionWithExpiry(Guid householdId, DateTimeOffset consentExpiresAtUtc)
    {
        var connection = BankConnection.CreatePending(
            householdId, Guid.NewGuid(), "ext-user-1", "session-1", NowUtc.AddDays(-30));
        connection.Activate($"conn-{connection.Id.Value:N}", "Test Bank", NowUtc.AddDays(-30).AddMinutes(5));
        connection.UpdateConsentExpiry(consentExpiresAtUtc, NowUtc.AddDays(-30).AddMinutes(5));
        return connection;
    }
}
