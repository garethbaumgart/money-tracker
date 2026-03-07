using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class BankConnectionEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BankConnectionEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostLinkSession_WithValidAuth_Returns200WithConsentUrl()
    {
        // P3-1-COMP-01: POST /bank/link-session with valid auth returns 200 with consent URL
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household first
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Create link session
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);

        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);
        Assert.False(string.IsNullOrWhiteSpace(linkPayload["consentUrl"]?.GetValue<string>()));
        Assert.NotNull(linkPayload["connectionId"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostCallback_WithValidConnection_Returns200WithActiveConnection()
    {
        // P3-1-COMP-02: POST /bank/callback with valid connection returns 200 with Active connection
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Create link session
        using var linkResponse = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId });
        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);

        var linkPayload = JsonNode.Parse(await linkResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(linkPayload);

        // Extract the consent session ID from the consent URL returned by the in-memory adapter.
        // The stub adapter returns URLs in the form https://consent.example.com/{sessionId}.
        var consentUrl = linkPayload["consentUrl"]!.GetValue<string>();
        var consentSessionId = consentUrl.Split('/').Last();

        // Call POST /bank/callback with the consent session ID
        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId });
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        var callbackPayload = JsonNode.Parse(await callbackResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(callbackPayload);
        Assert.Equal("Active", callbackPayload["status"]?.GetValue<string>());
        Assert.Equal(householdId.ToString(), callbackPayload["householdId"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostCallback_WithInvalidSession_Returns404WithTypedError()
    {
        // P3-1-COMP-03: POST /bank/callback with invalid session returns 404 with typed error
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId = "nonexistent-session-id" });

        Assert.Equal(HttpStatusCode.NotFound, callbackResponse.StatusCode);
        Assert.Equal("application/problem+json", callbackResponse.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await callbackResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("bank_connection_not_found", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetConnections_WithValidHousehold_ReturnsConnectionList()
    {
        // P3-1-COMP-04: GET /bank/connections with valid household returns connection list
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        // Create a household
        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);
        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        // Get connections (should be empty initially)
        using var connectionsResponse = await client.GetAsync($"/bank/connections?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, connectionsResponse.StatusCode);

        var payload = JsonNode.Parse(await connectionsResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        var connections = payload["connections"]!.AsArray();
        Assert.NotNull(connections);
        Assert.Empty(connections);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostLinkSession_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/bank/link-session",
            new { householdId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetConnections_WithoutHouseholdId_Returns400()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var response = await client.GetAsync("/bank/connections");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("validation_error", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostCallback_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        using var callbackResponse = await client.PostAsJsonAsync(
            "/bank/callback",
            new { consentSessionId = "some-session-id" });

        Assert.Equal(HttpStatusCode.Unauthorized, callbackResponse.StatusCode);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostCallback_WithEmptyBody_Returns400()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var emptyBodyContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/bank/callback", emptyBodyContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
