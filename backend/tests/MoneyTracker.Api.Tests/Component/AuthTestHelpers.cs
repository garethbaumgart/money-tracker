using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MoneyTracker.Api.Tests.Component;

internal static class AuthTestHelpers
{
    public static async Task<string> GetAccessTokenAsync(HttpClient client, string email)
    {
        var requestCode = await client.PostAsJsonAsync("/auth/request-code", new { email });
        requestCode.EnsureSuccessStatusCode();

        var requestCodePayload = await requestCode.Content.ReadFromJsonAsync<AuthChallengeResponse>();
        if (requestCodePayload is null || string.IsNullOrWhiteSpace(requestCodePayload.ChallengeToken))
        {
            throw new InvalidOperationException("Request-code did not return a challenge token.");
        }

        var verifyRequest = await client.PostAsJsonAsync(
            "/auth/verify-code",
            new { email, challengeToken = requestCodePayload.ChallengeToken });
        verifyRequest.EnsureSuccessStatusCode();

        var verifyPayload = await verifyRequest.Content.ReadFromJsonAsync<AuthVerifyResponse>();
        if (verifyPayload is null || string.IsNullOrWhiteSpace(verifyPayload.AccessToken))
        {
            throw new InvalidOperationException("Verify-code did not return an access token.");
        }

        return verifyPayload.AccessToken;
    }

    public static void SetBearer(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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
