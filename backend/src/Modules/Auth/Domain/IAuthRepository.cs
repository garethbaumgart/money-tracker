namespace MoneyTracker.Modules.Auth.Domain;

public interface IAuthRepository
{
    Task AddChallengeAsync(AuthChallenge challenge, CancellationToken cancellationToken);

    Task<AuthChallenge?> GetChallengeAsync(string challengeToken, CancellationToken cancellationToken);

    Task<AuthUser> GetOrCreateUserAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task AddSessionAsync(AuthSession session, CancellationToken cancellationToken);

    Task<AuthSession?> GetSessionByAccessTokenAsync(string accessToken, CancellationToken cancellationToken);

    Task<AuthSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task ReplaceSessionAsync(
        string oldRefreshToken,
        string oldAccessToken,
        AuthSession newSession,
        CancellationToken cancellationToken);

    Task RemoveSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}
