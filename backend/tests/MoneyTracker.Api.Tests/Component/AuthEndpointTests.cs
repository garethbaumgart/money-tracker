using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class AuthEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public AuthEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostAuthRequestCode_ReturnsCreated_WithChallengePayload()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/auth/request-code", new { email = "user-request-code@example.com" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload["challengeToken"]?.GetValue<string>()));
        Assert.NotNull(payload["expiresAtUtc"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostAuthVerifyCode_ReturnsUnauthorized_WhenCodeIsInvalid()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/verify-code",
            new
            {
                email = "invalid-code@example.com",
                challengeToken = "does-not-exist"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("auth_challenge_not_found", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostAuthRefresh_ReturnsUnauthorized_WhenRefreshTokenInvalid()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "invalid-refresh-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("auth_refresh_token_invalid", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostAuthVerifyCode_ReturnsTokens_WhenCodeIsValid()
    {
        using var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        using var requestCode = await client.PostAsJsonAsync("/auth/request-code", new { email });
        requestCode.EnsureSuccessStatusCode();
        var challengePayload = JsonNode.Parse(await requestCode.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(challengePayload);

        var challengeToken = challengePayload["challengeToken"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(challengeToken));

        using var response = await client.PostAsJsonAsync(
            "/auth/verify-code",
            new { email, challengeToken = challengeToken });
        response.EnsureSuccessStatusCode();

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload["accessToken"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(payload["refreshToken"]?.GetValue<string>()));
    }
}
