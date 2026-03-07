using MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;
using MoneyTracker.Modules.BankConnections.Domain;
using MoneyTracker.Modules.BankConnections.Infrastructure;

namespace MoneyTracker.Modules.BankConnections.Tests.Application;

public sealed class GetPilotMetricsHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MixedAuNzEvents_CorrectSuccessRatesPerRegion()
    {
        // P3-4-UNIT-03: Metrics aggregation with mixed AU/NZ events -> correct success rates per region
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        // Add AU sync events: 9 success, 1 failure = 90% success rate
        for (var i = 0; i < 9; i++)
        {
            await syncEventRepo.AddAsync(
                SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 1000, 5, null, NowUtc.AddDays(-i)),
                CancellationToken.None);
        }
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Failed, 2000, 0, "ProviderError", NowUtc.AddDays(-10)),
            CancellationToken.None);

        // Add NZ sync events: 7 success, 3 failure = 70% success rate
        for (var i = 0; i < 7; i++)
        {
            await syncEventRepo.AddAsync(
                SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Success, 1500, 3, null, NowUtc.AddDays(-i)),
                CancellationToken.None);
        }
        for (var i = 0; i < 3; i++)
        {
            await syncEventRepo.AddAsync(
                SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Failed, 3000, 0, "Timeout", NowUtc.AddDays(-i - 7)),
                CancellationToken.None);
        }

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30), CancellationToken.None);

        Assert.True(result.IsSuccess);

        // Overall: 16/20 = 0.8
        Assert.Equal(0.8, result.SyncMetrics!.OverallSuccessRate, 3);

        // AU: 9/10 = 0.9
        var auMetric = result.SyncMetrics.ByRegion.Single(r => r.Region == "AU");
        Assert.Equal(0.9, auMetric.SuccessRate, 3);

        // NZ: 7/10 = 0.7
        var nzMetric = result.SyncMetrics.ByRegion.Single(r => r.Region == "NZ");
        Assert.Equal(0.7, nzMetric.SuccessRate, 3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task VaryingDurations_CorrectAveragesPerRegion()
    {
        // P3-4-UNIT-04: Latency aggregation with varying durations -> correct averages per region
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        // AU events with latencies: 1000, 2000, 3000 -> avg = 2000
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 1000, 5, null, NowUtc.AddDays(-1)),
            CancellationToken.None);
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 2000, 3, null, NowUtc.AddDays(-2)),
            CancellationToken.None);
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 3000, 2, null, NowUtc.AddDays(-3)),
            CancellationToken.None);

        // NZ events with latencies: 5000, 10000 -> avg = 7500
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Success, 5000, 3, null, NowUtc.AddDays(-1)),
            CancellationToken.None);
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Success, 10000, 2, null, NowUtc.AddDays(-2)),
            CancellationToken.None);

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var auMetric = result.SyncMetrics!.ByRegion.Single(r => r.Region == "AU");
        Assert.Equal(2000.0, auMetric.AvgLatencyMs, 1);

        var nzMetric = result.SyncMetrics.ByRegion.Single(r => r.Region == "NZ");
        Assert.Equal(7500.0, nzMetric.AvgLatencyMs, 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EmptyEvents_ReturnsZeroMetrics()
    {
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.0, result.SyncMetrics!.OverallSuccessRate);
        Assert.Empty(result.SyncMetrics.ByRegion);
        Assert.Empty(result.SyncMetrics.ByInstitution);
        Assert.Empty(result.LinkMetrics!.ByInstitution);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InstitutionCoverage_CalculatedFromLinkEvents()
    {
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        // 5 CBA link attempts, 4 successful
        for (var i = 0; i < 4; i++)
        {
            await linkEventRepo.AddAsync(
                LinkEvent.Create("CBA", "AU", EventOutcome.Success, 1000, null, NowUtc.AddDays(-i)),
                CancellationToken.None);
        }
        await linkEventRepo.AddAsync(
            LinkEvent.Create("CBA", "AU", EventOutcome.Failed, 2000, "ProviderError", NowUtc.AddDays(-5)),
            CancellationToken.None);

        // 3 ANZ NZ link attempts, 1 successful
        await linkEventRepo.AddAsync(
            LinkEvent.Create("ANZ NZ", "NZ", EventOutcome.Success, 1500, null, NowUtc.AddDays(-1)),
            CancellationToken.None);
        await linkEventRepo.AddAsync(
            LinkEvent.Create("ANZ NZ", "NZ", EventOutcome.Failed, 3000, "Timeout", NowUtc.AddDays(-2)),
            CancellationToken.None);
        await linkEventRepo.AddAsync(
            LinkEvent.Create("ANZ NZ", "NZ", EventOutcome.Failed, 3000, "Timeout", NowUtc.AddDays(-3)),
            CancellationToken.None);

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var cbaLink = result.LinkMetrics!.ByInstitution.Single(i => i.Institution == "CBA");
        Assert.Equal(5, cbaLink.Attempted);
        Assert.Equal(4, cbaLink.Successful);

        var anzLink = result.LinkMetrics.ByInstitution.Single(i => i.Institution == "ANZ NZ");
        Assert.Equal(3, anzLink.Attempted);
        Assert.Equal(1, anzLink.Successful);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegionFilter_OnlyReturnsFilteredData()
    {
        var syncEventRepo = new StubSyncEventRepository();
        var linkEventRepo = new StubLinkEventRepository();
        var connectionRepo = new InMemoryBankConnectionRepository();

        // Add AU and NZ events
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 1000, 5, null, NowUtc.AddDays(-1)),
            CancellationToken.None);
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Success, 2000, 3, null, NowUtc.AddDays(-1)),
            CancellationToken.None);

        var handler = new GetPilotMetricsHandler(
            syncEventRepo, linkEventRepo, connectionRepo,
            new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new GetPilotMetricsQuery(PeriodDays: 30, Region: "NZ"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.SyncMetrics!.ByRegion);
        Assert.Equal("NZ", result.SyncMetrics.ByRegion.First().Region);
    }
}
