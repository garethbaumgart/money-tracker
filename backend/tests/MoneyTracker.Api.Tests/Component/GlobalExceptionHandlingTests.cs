using System.Net;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class GlobalExceptionHandlingTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly HttpClient _client;

    public GlobalExceptionHandlingTests(MoneyTrackerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task UnhandledException_ReturnsProblemDetailsContract()
    {
        using var response = await _client.GetAsync("/__test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(json);
        Assert.Equal(500, json["status"]?.GetValue<int>());
        Assert.Equal("/__test/throw", json["instance"]?.GetValue<string>());
        Assert.Equal("internal_server_error", json["code"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(json["title"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(json["type"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(json["detail"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(json["traceId"]?.GetValue<string>()));
    }
}
