using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Tests.Component;

public sealed class GlobalExceptionHandlingTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public GlobalExceptionHandlingTests(MoneyTrackerApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Component")]
    public async Task UnhandledException_ReturnsProblemDetailsContract()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/__test/throw");

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
        Assert.False(string.IsNullOrWhiteSpace(json["correlationId"]?.GetValue<string>()));
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task UnhandledException_UsesProvidedCorrelationHeaderForProblemDetailsAndResponseHeader()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/__test/throw");
        request.Headers.Add(CorrelationHeaders.CorrelationIdHeader, "abc-123");

        using var response = await client.SendAsync(request);

        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(json);
        Assert.Equal("abc-123", json["correlationId"]?.GetValue<string>());

        Assert.True(response.Headers.TryGetValues(
            CorrelationHeaders.CorrelationIdHeader,
            out var responseCorrelationValues));
        Assert.Equal("abc-123", responseCorrelationValues.Single());
    }
}
