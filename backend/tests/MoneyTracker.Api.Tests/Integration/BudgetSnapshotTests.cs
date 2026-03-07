using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MoneyTracker.Api.Tests.Component;

namespace MoneyTracker.Api.Tests.Integration;

public sealed class BudgetSnapshotTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BudgetSnapshotTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BudgetSnapshot_ReturnsTotalsForCurrentPeriod()
    {
        var fixedNow = DateTimeOffset.Parse("2026-03-01T12:00:00Z");
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(fixedNow));
            });
        }).CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        var householdName = $"Household-{Guid.NewGuid():N}";
        using var householdResponse = await client.PostAsJsonAsync("/households", new { name = householdName });
        Assert.Equal(HttpStatusCode.Created, householdResponse.StatusCode);

        var householdPayload = JsonNode.Parse(await householdResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(householdPayload);
        var householdId = Guid.Parse(householdPayload["id"]!.GetValue<string>());

        using var categoryResponse = await client.PostAsJsonAsync(
            "/budgets/categories",
            new { householdId, name = "Groceries" });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var categoryPayload = JsonNode.Parse(await categoryResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(categoryPayload);
        var categoryId = Guid.Parse(categoryPayload["id"]!.GetValue<string>());

        using var allocationResponse = await client.PostAsJsonAsync(
            "/budgets",
            new { householdId, categoryId, amount = 500m });
        Assert.Equal(HttpStatusCode.OK, allocationResponse.StatusCode);

        var occurredAtUtc = fixedNow;
        using var transactionResponse = await client.PostAsJsonAsync(
            "/transactions",
            new
            {
                householdId,
                amount = 120m,
                occurredAtUtc,
                description = "Market",
                categoryId
            });
        Assert.Equal(HttpStatusCode.Created, transactionResponse.StatusCode);

        using var snapshotResponse = await client.GetAsync($"/budgets/current?householdId={householdId}");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);

        var snapshotPayload = JsonNode.Parse(await snapshotResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(snapshotPayload);

        Assert.Equal(500m, snapshotPayload["totalAllocated"]!.GetValue<decimal>());
        Assert.Equal(120m, snapshotPayload["totalSpent"]!.GetValue<decimal>());
        Assert.Equal(380m, snapshotPayload["totalRemaining"]!.GetValue<decimal>());
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
