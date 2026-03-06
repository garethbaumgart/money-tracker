namespace MoneyTracker.Modules.Auth.Application.VerifyCode;

public sealed record AuthTokenSet(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
