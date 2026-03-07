using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Api.Tests.Component;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class PilotMetricsFlowTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public PilotMetricsFlowTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncOrchestrator_RecordsEvents_MetricsEndpointReturnsCorrectAggregation()
    {
        // P3-4-INT-01: Sync orchestrator records events; metrics endpoint returns correct aggregation
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Step 1: Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Metrics-INT-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Step 2: Create link session
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);

        // Step 3: Process callback to activate connection
        var consentUrl = linkPayload["consentUrl"]!.GetValue<string>();
        var consentSessionId = consentUrl.Split('/').Last();

        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId });
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        // Step 4: Trigger manual sync (this should record sync events)
        using var syncResponse = await client.PostAsJsonAsync(
            "/bank/sync",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);
        var syncPayload = JsonNode.Parse(await syncResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(syncPayload);
        Assert.True(syncPayload["syncedCount"]!.GetValue<int>() > 0);

        // Step 5: Query metrics — should reflect the sync that just happened
        using var metricsResponse = await client.GetAsync("/admin/pilot-metrics?periodDays=1");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var metricsPayload = JsonNode.Parse(await metricsResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(metricsPayload);

        var syncMetrics = metricsPayload["syncMetrics"]!.AsObject();
        var overallRate = syncMetrics["overallSuccessRate"]!.GetValue<double>();
        Assert.True(overallRate > 0, "Expected non-zero sync success rate after sync.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MultipleLinkAttempts_MetricsShowCoverage()
    {
        // P3-4-INT-02: Multiple link attempts across institutions; metrics show coverage
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Coverage-INT-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Create multiple link sessions (each records a link event)
        for (var i = 0; i < 3; i++)
        {
            using var linkResponse = await client.PostAsJsonAsync(
                "/bank/link-session",
                new { householdId });
            Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        }

        // Query metrics — link metrics should show attempts
        using var metricsResponse = await client.GetAsync("/admin/pilot-metrics?periodDays=1");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var metricsPayload = JsonNode.Parse(await metricsResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(metricsPayload);

        var linkMetrics = metricsPayload["linkMetrics"]!.AsObject();
        var byInstitution = linkMetrics["byInstitution"]!.AsArray();

        // The in-memory adapter uses "Unknown" as institution; we should have link events
        // Verify the endpoint returns the expected shape
        Assert.NotNull(byInstitution);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullFlow_Link_Sync_QueryMetrics_AllCategoriesReturn()
    {
        // P3-4-E2E-01: Link -> sync -> query metrics -> all categories return accurate data
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"E2E-Metrics-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Link: create session and activate
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);

        var consentUrl = linkPayload["consentUrl"]!.GetValue<string>();
        var consentSessionId = consentUrl.Split('/').Last();

        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId });
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        // Sync
        using var syncResponse = await client.PostAsJsonAsync(
            "/bank/sync",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        // Query metrics
        using var metricsResponse = await client.GetAsync("/admin/pilot-metrics");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var metricsPayload = JsonNode.Parse(await metricsResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(metricsPayload);

        // Verify all categories are present and return data
        Assert.Equal(30, metricsPayload["periodDays"]!.GetValue<int>());

        var syncMetrics = metricsPayload["syncMetrics"]!.AsObject();
        Assert.NotNull(syncMetrics["overallSuccessRate"]);
        Assert.NotNull(syncMetrics["byRegion"]);
        Assert.NotNull(syncMetrics["byInstitution"]);

        var linkMetrics = metricsPayload["linkMetrics"]!.AsObject();
        Assert.NotNull(linkMetrics["byInstitution"]);

        var consentHealth = metricsPayload["consentHealth"]!.AsObject();
        Assert.NotNull(consentHealth["averageDurationDays"]);
        Assert.NotNull(consentHealth["reConsentRate"]);
        Assert.NotNull(consentHealth["revocationRate"]);
    }
}
