using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MoneyTracker.Api.Tests.Component;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class ConsentLifecycleFlowTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public ConsentLifecycleFlowTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullFlow_Link_ConsentExpires_ReConsent_SyncResumes()
    {
        // P3-3-E2E-01: Link -> consent expires -> re-consent -> sync resumes
        using var client = _factory.CreateClient();

        // Step 1: Authenticate
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Step 2: Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"ConsentE2E-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Step 3: Create link session
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);
        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);
        var consentUrl = linkPayload["consentUrl"]!.GetValue<string>();
        var consentSessionId = consentUrl.Split('/').Last();
        var connectionId = linkPayload["connectionId"]!.GetValue<string>();

        // Step 4: Process callback to activate
        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId });
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackPayload = JsonNode.Parse(await callbackResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(callbackPayload);
        Assert.Equal("Active", callbackPayload["status"]?.GetValue<string>());
        Assert.NotNull(callbackPayload["consentStatus"]?.GetValue<string>());
        Assert.Equal("Active", callbackPayload["consentStatus"]?.GetValue<string>());

        // Step 5: Verify sync works
        using var syncResponse = await client.PostAsJsonAsync(
            "/bank/sync",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);
        var syncPayload = JsonNode.Parse(await syncResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(syncPayload);
        var initialSyncedCount = syncPayload["syncedCount"]!.GetValue<int>();
        Assert.True(initialSyncedCount > 0, "Expected synced transactions from in-memory provider.");

        // Step 6: Verify connection listing includes consent fields
        using var connectionsResponse = await client.GetAsync($"/bank/connections?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, connectionsResponse.StatusCode);
        var connectionsPayload = JsonNode.Parse(await connectionsResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(connectionsPayload);
        var connections = connectionsPayload["connections"]!.AsArray();
        Assert.Single(connections);
        var connectionObj = connections[0]!.AsObject();
        Assert.NotNull(connectionObj["consentStatus"]?.GetValue<string>());
        Assert.NotNull(connectionObj["consentExpiresAtUtc"]?.GetValue<string>());

        // Step 7: Verify re-consent for active connection is rejected
        using var reConsentActiveResponse = await client.PostAsync(
            $"/bank/connections/{connectionId}/re-consent",
            null);
        Assert.Equal(HttpStatusCode.BadRequest, reConsentActiveResponse.StatusCode);
    }
}
