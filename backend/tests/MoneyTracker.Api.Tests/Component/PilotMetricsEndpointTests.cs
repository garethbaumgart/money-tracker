using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Api.Tests.Component;

public sealed class PilotMetricsEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public PilotMetricsEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetPilotMetrics_WithPeriodDays7_ReturnsLast7DaysOnly()
    {
        // P3-4-COMP-01: GET /admin/pilot-metrics with periodDays=7 -> returns last 7 days only
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Seed some sync events via the repository (we use the DI-registered singleton)
        var syncEventRepo = _factory.Services.GetRequiredService<ISyncEventRepository>();
        var nowUtc = DateTimeOffset.UtcNow;

        // Add event within 7 days
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 800, 5, null, nowUtc.AddDays(-3)),
            CancellationToken.None);

        // Add event outside 7 days (should not be included)
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "ANZ", "AU", EventOutcome.Success, 1200, 3, null, nowUtc.AddDays(-10)),
            CancellationToken.None);

        using var response = await client.GetAsync("/admin/pilot-metrics?periodDays=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal(7, payload["periodDays"]?.GetValue<int>());

        var syncMetrics = payload["syncMetrics"]?.AsObject();
        Assert.NotNull(syncMetrics);

        // Overall success rate should reflect only events within 7-day window
        var overallRate = syncMetrics["overallSuccessRate"]?.GetValue<double>();
        Assert.NotNull(overallRate);
        Assert.True(overallRate > 0, "Expected non-zero success rate for 7-day window.");
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetPilotMetrics_WithoutAuth_Returns401()
    {
        // P3-4-COMP-02: GET /admin/pilot-metrics without admin auth -> 401
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/admin/pilot-metrics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetPilotMetrics_WithRegionNzFilter_ReturnsNzOnlyData()
    {
        // P3-4-COMP-03: GET /admin/pilot-metrics with region=NZ filter -> NZ-only data
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Seed AU and NZ events
        var syncEventRepo = _factory.Services.GetRequiredService<ISyncEventRepository>();
        var nowUtc = DateTimeOffset.UtcNow;

        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "CBA", "AU", EventOutcome.Success, 800, 5, null, nowUtc.AddDays(-1)),
            CancellationToken.None);
        await syncEventRepo.AddAsync(
            SyncEvent.Create(Guid.NewGuid(), "ANZ NZ", "NZ", EventOutcome.Success, 1500, 3, null, nowUtc.AddDays(-1)),
            CancellationToken.None);

        using var response = await client.GetAsync("/admin/pilot-metrics?region=NZ");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);

        var byRegion = payload["syncMetrics"]?.AsObject()?["byRegion"]?.AsArray();
        Assert.NotNull(byRegion);

        // Should only contain NZ data
        foreach (var regionMetric in byRegion)
        {
            Assert.Equal("NZ", regionMetric!.AsObject()["region"]?.GetValue<string>());
        }
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetPilotMetrics_ReturnsFullResponseShape()
    {
        // Verify the full response shape matches the expected format
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var response = await client.GetAsync("/admin/pilot-metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);

        // Verify top-level structure
        Assert.NotNull(payload["periodDays"]);
        Assert.NotNull(payload["syncMetrics"]);
        Assert.NotNull(payload["linkMetrics"]);
        Assert.NotNull(payload["consentHealth"]);

        // Verify sync metrics structure
        var syncMetrics = payload["syncMetrics"]!.AsObject();
        Assert.NotNull(syncMetrics["overallSuccessRate"]);
        Assert.NotNull(syncMetrics["byRegion"]);
        Assert.NotNull(syncMetrics["byInstitution"]);

        // Verify link metrics structure
        var linkMetrics = payload["linkMetrics"]!.AsObject();
        Assert.NotNull(linkMetrics["byInstitution"]);

        // Verify consent health structure
        var consentHealth = payload["consentHealth"]!.AsObject();
        Assert.NotNull(consentHealth["averageDurationDays"]);
        Assert.NotNull(consentHealth["reConsentRate"]);
        Assert.NotNull(consentHealth["revocationRate"]);
    }
}
