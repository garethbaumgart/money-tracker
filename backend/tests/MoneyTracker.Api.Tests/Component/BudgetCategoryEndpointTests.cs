using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class BudgetCategoryEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BudgetCategoryEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostBudgetCategories_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();
        var request = new { householdId = Guid.NewGuid(), name = "Groceries" };

        using var response = await client.PostAsJsonAsync("/budgets/categories", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        var code = payload["code"]?.GetValue<string>();
        Assert.NotNull(code);
        Assert.Contains(code, new[]
        {
            "auth_access_token_missing",
            "auth_access_token_invalid"
        });
    }
}
