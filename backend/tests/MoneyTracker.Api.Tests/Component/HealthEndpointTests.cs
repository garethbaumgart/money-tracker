using System.Net;
using System.Linq;
using System.Net.Http.Json;
using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Tests.Component;

public sealed class HealthEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;
    private const int OversizedCorrelationIdLength = 129;

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
    public async Task GetHealth_PropagatesProvidedCorrelationHeader()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationHeaders.CorrelationIdHeader, "abc-123");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(
            CorrelationHeaders.CorrelationIdHeader,
            out var headerValues));
        Assert.Equal("abc-123", headerValues.Single());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetHealth_GeneratesCorrelationHeaderWhenMissing()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(
            CorrelationHeaders.CorrelationIdHeader,
            out var headerValues));
        Assert.Single(headerValues);
        var correlationId = headerValues.Single();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    public static IEnumerable<object[]> InvalidCorrelationIds =>
        new[]
        {
            new object[] { string.Empty },
            new object[] { "   " },
            new object[] { new string('a', OversizedCorrelationIdLength) }
        };

    [Theory]
    [MemberData(nameof(InvalidCorrelationIds))]
    [Trait("Category", "Component")]
    public async Task GetHealth_GeneratesCorrelationHeaderWhenGivenInvalidValue(
        string invalidCorrelationId
    )
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationHeaders.CorrelationIdHeader, invalidCorrelationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(
            CorrelationHeaders.CorrelationIdHeader,
            out var headerValues));
        var correlationId = headerValues.Single();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual(invalidCorrelationId, correlationId);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task GetHealth_GeneratesCorrelationHeaderWhenMultipleValuesProvided()
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(
            CorrelationHeaders.CorrelationIdHeader,
            new[] { "abc-123", "def-456" });

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(
            CorrelationHeaders.CorrelationIdHeader,
            out var headerValues));
        var correlationId = headerValues.Single();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.NotEqual("abc-123", correlationId);
        Assert.NotEqual("def-456", correlationId);
        Assert.NotEqual("abc-123,def-456", correlationId);
    }
}
