using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class ConsentExpiryNonFunctionalTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ConsentCheckWith1000Connections_CompletesFast_NoDuplicateNotifications()
    {
        // P3-3-NF-01: Consent check with 1000 connections -> completes fast, no duplicate notifications
        var connectionRepo = new StubBankConnectionRepository();
        var notificationSender = new InMemoryConsentNotificationSender();

        // Create 1000 connections with various expiry dates
        for (var i = 0; i < 1000; i++)
        {
            var householdId = Guid.NewGuid();
            BankConnection connection;

            if (i % 4 == 0)
            {
                // 250 connections expiring in 3 days -> should trigger ExpiringSoon
                connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(3));
            }
            else if (i % 4 == 1)
            {
                // 250 connections expired 2 days ago -> should trigger Expired
                connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(-2));
            }
            else if (i % 4 == 2)
            {
                // 250 connections expiring in 60 days -> no action
                connection = CreateActiveConnectionWithExpiry(householdId, NowUtc.AddDays(60));
            }
            else
            {
                // 250 connections with no expiry -> no action
                connection = BankConnection.CreatePending(
                    householdId, Guid.NewGuid(), $"ext-user-{i}", $"session-{i}", NowUtc.AddDays(-30));
                connection.Activate($"conn-{connection.Id.Value:N}", "Test Bank", NowUtc.AddDays(-30).AddMinutes(5));
            }

            await connectionRepo.AddAsync(connection, CancellationToken.None);
        }

        var handler = new CheckConsentExpiryHandler(
            connectionRepo,
            notificationSender,
            new StubTimeProvider(NowUtc),
            NullLogger<CheckConsentExpiryHandler>.Instance);

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.HandleAsync(
            new CheckConsentExpiryCommand(),
            CancellationToken.None);
        stopwatch.Stop();

        // Verify it completes within a reasonable time (< 5 seconds)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Consent check took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

        // 250 expiring soon + 250 expired = 500 total notifications
        Assert.Equal(250, result.ExpiringSoonCount);
        Assert.Equal(250, result.ExpiredCount);
        Assert.Equal(500, result.NotificationsCreated);

        // Verify no duplicate notifications
        var notifications = notificationSender.GetAll();
        Assert.Equal(500, notifications.Count);

        var uniqueConnectionIds = notifications
            .Select(n => n.ConnectionId)
            .Distinct()
            .Count();
        Assert.Equal(500, uniqueConnectionIds); // Each connection gets exactly one notification
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
