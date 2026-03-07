using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoneyTracker.Api.Tests.Component;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class HouseholdDashboardTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public HouseholdDashboardTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HouseholdDashboard_ReturnsSameSnapshot_ForMembers_AndRejectsNonMembers()
    {
        var fixedNow = DateTimeOffset.Parse("2026-03-05T12:00:00Z");
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(fixedNow));
            });
        }).CreateClient();

        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var ownerToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, ownerToken);

        using var householdResponse = await client.PostAsJsonAsync(
            "/households",
            new { name = $"Household-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);

        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        var memberEmail = $"{Guid.NewGuid():N}@example.com";
        using var inviteResponse = await client.PostAsJsonAsync(
            $"/households/{householdId}/invite",
            new { inviteeEmail = memberEmail });
        inviteResponse.EnsureSuccessStatusCode();
        var invitePayload = JsonNode.Parse(await inviteResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(invitePayload);
        var invitationToken = invitePayload["invitationToken"]!.GetValue<string>();

        var memberToken = await AuthTestHelpers.GetAccessTokenAsync(client, memberEmail);
        AuthTestHelpers.SetBearer(client, memberToken);
        using var acceptResponse = await client.PostAsync(
            $"/households/invitations/{invitationToken}/accept",
            null);
        acceptResponse.EnsureSuccessStatusCode();

        AuthTestHelpers.SetBearer(client, ownerToken);
        using var categoryResponse = await client.PostAsJsonAsync(
            "/budgets/categories",
            new { householdId, name = "Groceries" });
        categoryResponse.EnsureSuccessStatusCode();
        var categoryPayload = JsonNode.Parse(await categoryResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(categoryPayload);
        var categoryId = Guid.Parse(categoryPayload["id"]!.GetValue<string>());

        using var allocationResponse = await client.PostAsJsonAsync(
            "/budgets",
            new { householdId, categoryId, amount = 500m });
        allocationResponse.EnsureSuccessStatusCode();

        using var transactionResponse = await client.PostAsJsonAsync(
            "/transactions",
            new
            {
                householdId,
                amount = 120m,
                occurredAtUtc = fixedNow,
                description = "Market",
                categoryId
            });
        transactionResponse.EnsureSuccessStatusCode();

        var ownerDashboard = await GetDashboardAsync(client, householdId);
        Assert.Equal(500m, ownerDashboard["totalAllocated"]!.GetValue<decimal>());
        Assert.Equal(120m, ownerDashboard["totalSpent"]!.GetValue<decimal>());
        Assert.Equal(380m, ownerDashboard["totalRemaining"]!.GetValue<decimal>());

        var ownerTransactions = ownerDashboard["recentTransactions"]?.AsArray();
        Assert.NotNull(ownerTransactions);
        Assert.Single(ownerTransactions);
        Assert.Equal(120m, ownerTransactions[0]!["amount"]!.GetValue<decimal>());

        AuthTestHelpers.SetBearer(client, memberToken);
        var memberDashboard = await GetDashboardAsync(client, householdId);
        Assert.Equal(ownerDashboard["totalAllocated"]!.GetValue<decimal>(), memberDashboard["totalAllocated"]!.GetValue<decimal>());
        Assert.Equal(ownerDashboard["totalSpent"]!.GetValue<decimal>(), memberDashboard["totalSpent"]!.GetValue<decimal>());
        Assert.Equal(ownerDashboard["totalRemaining"]!.GetValue<decimal>(), memberDashboard["totalRemaining"]!.GetValue<decimal>());

        var nonMemberToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, nonMemberToken);
        using var forbiddenResponse = await client.GetAsync($"/households/{householdId}/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private static async Task<JsonObject> GetDashboardAsync(HttpClient client, Guid householdId)
    {
        using var response = await client.GetAsync($"/households/{householdId}/dashboard");
        response.EnsureSuccessStatusCode();
        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        return payload!;
    }
}
