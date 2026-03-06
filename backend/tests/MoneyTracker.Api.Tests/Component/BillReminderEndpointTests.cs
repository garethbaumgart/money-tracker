using System.Net;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class BillReminderEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public BillReminderEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetBillReminders_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();
        var householdId = Guid.NewGuid();

        using var response = await client.GetAsync($"/households/{householdId}/bill-reminders");

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
