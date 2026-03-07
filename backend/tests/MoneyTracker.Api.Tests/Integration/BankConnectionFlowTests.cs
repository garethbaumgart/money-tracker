using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MoneyTracker.Api.Tests.Component;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class BankConnectionFlowTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BankConnectionFlowTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FullFlow_AuthThenLinkSessionThenListConnections_PendingConnectionAppears()
    {
        // P3-1-E2E-01: Auth -> link-session -> callback -> list connections
        using var client = _factory.CreateClient();

        // Step 1: Authenticate
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Step 2: Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"BankTest-{Guid.NewGuid():N}" });
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
        Assert.False(string.IsNullOrWhiteSpace(linkPayload["consentUrl"]?.GetValue<string>()));

        // Step 4: List connections — the pending connection should appear
        using var listResponse = await client.GetAsync($"/bank/connections?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listPayload = JsonNode.Parse(await listResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(listPayload);
        var connections = listPayload["connections"]!.AsArray();
        Assert.Single(connections);

        var connection = connections[0]!.AsObject();
        Assert.Equal("Pending", connection["status"]?.GetValue<string>());
        Assert.Equal(householdId.ToString(), connection["householdId"]?.GetValue<string>());
    }
}
