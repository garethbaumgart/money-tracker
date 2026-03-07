using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Api.Tests.Component;

public sealed class HouseholdDashboardEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public HouseholdDashboardEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetDashboard_ReturnsForbidden_WhenUserNotMember()
    {
        using var client = _factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, ownerToken);

        using var createResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        createResponse.EnsureSuccessStatusCode();
        var createPayload = JsonNode.Parse(await createResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(createPayload);
        var householdId = createPayload["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(householdId));

        var nonMemberToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, nonMemberToken);
        using var response = await client.GetAsync($"/households/{householdId}/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal(HouseholdErrors.HouseholdAccessDenied, payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetDashboard_ReturnsUnauthorized_WhenTokenMissing()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync($"/households/{Guid.NewGuid()}/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetDashboard_ReturnsNotFound_WhenHouseholdMissing()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var response = await client.GetAsync($"/households/{Guid.NewGuid()}/dashboard");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal(HouseholdErrors.HouseholdNotFound, payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetDashboard_ReturnsOk_WhenUserIsMember()
    {
        using var client = _factory.CreateClient();
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, ownerToken);

        using var createResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        createResponse.EnsureSuccessStatusCode();
        var createPayload = JsonNode.Parse(await createResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(createPayload);
        var householdId = createPayload["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(householdId));

        using var response = await client.GetAsync($"/households/{householdId}/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboardPayload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(dashboardPayload);
        Assert.Equal(0m, dashboardPayload["totalAllocated"]!.GetValue<decimal>());
        Assert.Equal(0m, dashboardPayload["totalSpent"]!.GetValue<decimal>());
        Assert.Equal(0m, dashboardPayload["totalRemaining"]!.GetValue<decimal>());
        Assert.Equal(0m, dashboardPayload["uncategorizedSpent"]!.GetValue<decimal>());
        Assert.Empty(dashboardPayload["categories"]!.AsArray());
        Assert.Empty(dashboardPayload["recentTransactions"]!.AsArray());
    }
}
