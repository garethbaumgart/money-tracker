using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Api.Tests.Component;

public sealed class NotificationDeviceTokenTests : IClassFixture<MoneyTrackerApiFactory>
{
    private readonly MoneyTrackerApiFactory _factory;

    public NotificationDeviceTokenTests(MoneyTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Component")]
    public async Task RegisterDeviceToken_DeduplicatesByUserAndDevice()
    {
        using var client = _factory.CreateClient();
        var (userId, accessToken) = await AuthenticateAsync(client, $"{Guid.NewGuid():N}@example.com");
        AuthTestHelpers.SetBearer(client, accessToken);

        var request = new { deviceId = "device-1", token = "token-abc", platform = "ios" };
        using var firstResponse = await client.PostAsJsonAsync("/notifications/device-tokens", request);
        using var secondResponse = await client.PostAsJsonAsync("/notifications/device-tokens", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationTokenRepository>();
        var tokens = await repository.GetTokensForUsersAsync(new[] { userId }, CancellationToken.None);
        Assert.Single(tokens);
    }

    private static async Task<(Guid UserId, string AccessToken)> AuthenticateAsync(HttpClient client, string email)
    {
        var requestCode = await client.PostAsJsonAsync("/auth/request-code", new { email });
        requestCode.EnsureSuccessStatusCode();
        var challengePayload = await requestCode.Content.ReadFromJsonAsync<AuthChallengeResponse>();
        if (challengePayload is null)
        {
            throw new InvalidOperationException("Request-code did not return a challenge token.");
        }

        var verify = await client.PostAsJsonAsync(
            "/auth/verify-code",
            new { email, challengeToken = challengePayload.ChallengeToken });
        verify.EnsureSuccessStatusCode();
        var verifyPayload = await verify.Content.ReadFromJsonAsync<AuthVerifyResponse>();
        if (verifyPayload is null)
        {
            throw new InvalidOperationException("Verify-code did not return credentials.");
        }

        return (verifyPayload.UserId, verifyPayload.AccessToken);
    }

    private sealed record AuthChallengeResponse(string ChallengeToken, DateTimeOffset ExpiresAtUtc);

    private sealed record AuthVerifyResponse(
        Guid UserId,
        string Email,
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAtUtc);
}
