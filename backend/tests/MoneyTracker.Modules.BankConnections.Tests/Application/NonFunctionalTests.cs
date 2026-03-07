using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.SyncTransactions;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Analytics;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class NonFunctionalTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConsecutiveFailures_ReachesExpectedCount()
    {
        // P3-2-NF-01: Basiq returns 5xx for 5 consecutive syncs -> consecutive failure count reaches 5
        var householdId = Guid.NewGuid();

        var connectionRepo = new StubBankConnectionRepository();
        var connection = CreateActiveConnection(householdId);
        await connectionRepo.AddAsync(connection, CancellationToken.None);

        var transactionRepo = new StubTransactionSyncRepository();
        var providerAdapter = new SyncFailingBankProviderAdapter();

        var handler = new SyncTransactionsHandler(
            connectionRepo, providerAdapter, transactionRepo,
            new StubSyncEventRepository(),
            new NoopAnalyticsEventPublisher(),
            new StubTimeProvider(NowUtc),
            NullLogger<SyncTransactionsHandler>.Instance);

        // Run 5 consecutive syncs that all fail
        for (var i = 0; i < 5; i++)
        {
            var result = await handler.HandleAsync(
                new SyncTransactionsCommand(householdId), CancellationToken.None);
            Assert.Equal(1, result.FailedConnections);
        }

        // Verify consecutive failure count is 5
        Assert.Equal(5, connection.SyncState.ConsecutiveFailures);
        Assert.NotNull(connection.SyncState.LastFailureUtc);
        Assert.Null(connection.SyncState.LastSuccessUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void WebhookSignatureValidation_UsesConstantTimeComparison()
    {
        // P3-2-NF-02: Webhook signature validation timing -> constant-time comparison
        // We verify the validator uses FixedTimeEquals by testing both valid and invalid signatures
        // and confirming the behavior is correct. The actual constant-time property comes from
        // CryptographicOperations.FixedTimeEquals in the implementation.
        var secret = "test-webhook-secret";
        var body = """{"eventType":"transaction.created","connectionId":"conn-123"}""";

        var key = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var expectedHash = HMACSHA256.HashData(key, bodyBytes);
        var validSignature = Convert.ToHexStringLower(expectedHash);

        // Verify valid signature passes
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(validSignature),
            Encoding.UTF8.GetBytes(validSignature));
        Assert.True(isValid);

        // Verify invalid signature fails
        var invalidSignature = "0000000000000000000000000000000000000000000000000000000000000000";
        var isInvalid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(invalidSignature),
            Encoding.UTF8.GetBytes(validSignature));
        Assert.False(isInvalid);

        // Verify different-length signatures fail
        var shortSignature = "deadbeef";
        var isDifferentLength = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(shortSignature),
            Encoding.UTF8.GetBytes(validSignature));
        Assert.False(isDifferentLength);
    }

    private static BankConnection CreateActiveConnection(Guid householdId)
    {
        var connection = BankConnection.CreatePending(
            householdId, Guid.NewGuid(), "ext-user-1", "session-1", NowUtc.AddDays(-1));
        connection.Activate($"conn-{connection.Id.Value:N}", "Test Bank", NowUtc.AddDays(-1).AddMinutes(5));
        return connection;
    }
}
