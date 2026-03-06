using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace MoneyTracker.Api.Tests.Component;

public sealed class CreateHouseholdEndpointTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public CreateHouseholdEndpointTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholds_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();
        var request = new { name = $"Family-{Guid.NewGuid():N}" };

        using var response = await client.PostAsJsonAsync("/households", request);

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

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholds_ReturnsCreatedResponseAndLocation_WhenAuthenticated()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        var request = new { name = $"Family-{Guid.NewGuid():N}" };

        using var response = await client.PostAsJsonAsync("/households", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Headers.Location);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);

        Assert.False(string.IsNullOrWhiteSpace(payload["id"]?.GetValue<string>()));
        Assert.Equal(request.name, payload["name"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(payload["createdAtUtc"]?.GetValue<string>()));
        Assert.EndsWith(payload["id"]!.GetValue<string>(), response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholds_ReturnsBadRequest_WhenNameInvalid()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var response = await client.PostAsJsonAsync("/households", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("validation_error", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholds_ReturnsConflict_WhenNameAlreadyExistsCaseInsensitive()
    {
        using var client = _factory.CreateClient();
        var baseName = $"Shared-{Guid.NewGuid():N}";
        var ownerEmail = $"{Guid.NewGuid():N}@example.com";
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, ownerEmail);
        AuthTestHelpers.SetBearer(client, accessToken);

        using var firstResponse = await client.PostAsJsonAsync("/households", new { name = baseName });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var conflictResponse = await client.PostAsJsonAsync("/households", new { name = baseName.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal("application/problem+json", conflictResponse.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await conflictResponse.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("household_name_conflict", payload["code"]?.GetValue<string>());
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task PostHouseholds_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthTestHelpers.GetAccessTokenAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        using var emptyBodyContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/households", emptyBodyContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = JsonNode.Parse(await response.Content.ReadAsStringAsync())?.AsObject();
        Assert.NotNull(payload);
        Assert.Equal("validation_error", payload["code"]?.GetValue<string>());
    }
}
