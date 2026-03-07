using System.Diagnostics;
using MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class PilotMetricsNonFunctionalTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MetricsAggregation_Over10000SyncEvents_CompletesWithin2Seconds()
    {
        // P3-4-NF-01: Metrics aggregation over 10,000 sync events -> completes within 2 seconds
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        var institutions = new[] { "CBA", "ANZ", "Westpac", "NAB", "ANZ NZ", "BNZ", "Kiwibank", "ASB" };
        var regions = new[] { "AU", "NZ" };

        // Seed 10,000 sync events
        for (var i = 0; i < 10_000; i++)
        {
            var institution = institutions[i % institutions.Length];
            var region = regions[i % regions.Length];
            var outcome = i % 20 == 0 ? EventOutcome.Failed : EventOutcome.Success;
            var errorCategory = outcome == EventOutcome.Failed ? "ProviderError" : null;

            await syncEventRepo.AddAsync(
                SyncEvent.Create(
                    Guid.NewGuid(),
                    institution,
                    region,
                    outcome,
                    durationMs: 500 + (i % 5000),
                    transactionCount: i % 50,
                    errorCategory,
                    NowUtc.AddMinutes(-i)),
                CancellationToken.None);
        }

        // Seed 1,000 link events for good measure
        for (var i = 0; i < 1_000; i++)
        {
            var institution = institutions[i % institutions.Length];
            var region = regions[i % regions.Length];
            var outcome = i % 10 == 0 ? EventOutcome.Failed : EventOutcome.Success;
            var errorCategory = outcome == EventOutcome.Failed ? "LinkError" : null;

            await linkEventRepo.AddAsync(
                LinkEvent.Create(
                    institution,
                    region,
                    outcome,
                    durationMs: 1000 + (i % 3000),
                    errorCategory,
                    NowUtc.AddMinutes(-i)),
                CancellationToken.None);
        }

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var stopwatch = Stopwatch.StartNew();
        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30), CancellationToken.None);
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 2000,
            $"Metrics aggregation took {stopwatch.ElapsedMilliseconds}ms, exceeding the 2-second threshold.");

        // Verify correct aggregation
        Assert.True(result.SyncMetrics!.OverallSuccessRate > 0);
        Assert.True(result.SyncMetrics.ByRegion.Count > 0);
        Assert.True(result.SyncMetrics.ByInstitution.Count > 0);
        Assert.True(result.LinkMetrics!.ByInstitution.Count > 0);
    }
}
