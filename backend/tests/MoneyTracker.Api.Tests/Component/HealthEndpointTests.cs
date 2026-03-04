using System.Net;
using System.Net.Http.Json;

namespace MoneyTracker.Api.Tests.Component;

public sealed class HealthEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(MoneyTrackerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetHealth_ReturnsOkWithStablePayload()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.True(payload.TryGetValue("status", out var status));
        Assert.Equal("ok", status);
    }
}
