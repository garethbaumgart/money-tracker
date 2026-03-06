using System.Net;
using System.Text.Json;
using System.Net.Http.Json;

namespace MoneyTracker.Api.Tests.Component;

public sealed class HealthEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public HealthEndpointTests(MoneyTrackerApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetHealth_ReturnsOkWithStablePayload()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("ok", payload["status"]);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetOpenApi_ReturnsDocumentInTesting()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(payload);
        Assert.True(payload!.ContainsKey("openapi"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetOpenApi_NotAvailableInProduction()
    {
        using var productionFactory = new MoneyTrackerApiFactory("Production");
        using var client = productionFactory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
