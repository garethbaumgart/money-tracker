namespace MoneyTracker.Modules.Auth.Domain;

public sealed class AuthSession
{
    public AuthSession(
        Guid userId,
        string userEmail,
        string accessToken,
        DateTimeOffset accessTokenExpiresAtUtc,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAtUtc,
        DateTimeOffset issuedAtUtc)
    {
        UserId = userId;
        UserEmail = userEmail;
        AccessToken = accessToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        IssuedAtUtc = issuedAtUtc;
    }

    public Guid UserId { get; }
    public string UserEmail { get; }
    public string AccessToken { get; }
    public DateTimeOffset AccessTokenExpiresAtUtc { get; }
    public string RefreshToken { get; }
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; }
    public DateTimeOffset IssuedAtUtc { get; }
}
