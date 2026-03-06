using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.RefreshSession;

public sealed class RefreshSessionHandler(IAuthRepository repository, TimeProvider timeProvider)
{
    public async Task<RefreshSessionResult> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var normalizedRefreshToken = command.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedRefreshToken))
        {
            return RefreshSessionResult.Failure(AuthErrors.ValidationError, "Refresh token is required.");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var existingSession = await repository.GetSessionByRefreshTokenAsync(normalizedRefreshToken, cancellationToken);
        if (existingSession is null)
        {
            return RefreshSessionResult.Failure(AuthErrors.RefreshTokenInvalid, "Refresh token not found.");
        }

        if (existingSession.RefreshTokenExpiresAtUtc < nowUtc)
        {
            await repository.RemoveSessionByRefreshTokenAsync(existingSession.RefreshToken, cancellationToken);
            return RefreshSessionResult.Failure(AuthErrors.RefreshTokenExpired, "Refresh token expired.");
        }

        var accessToken = Guid.NewGuid().ToString("N");
        var refreshToken = Guid.NewGuid().ToString("N");
        var newSession = new AuthSession(
            existingSession.UserId,
            existingSession.UserEmail,
            accessToken,
            nowUtc.Add(AuthPolicy.AccessTokenLifetime),
            refreshToken,
            nowUtc.Add(AuthPolicy.RefreshTokenLifetime),
            nowUtc);

        await repository.ReplaceSessionAsync(
            existingSession.RefreshToken,
            existingSession.AccessToken,
            newSession,
            cancellationToken);

        return RefreshSessionResult.Success(new VerifyCode.AuthTokenSet(
            existingSession.UserId,
            existingSession.UserEmail,
            accessToken,
            newSession.AccessTokenExpiresAtUtc,
            refreshToken,
            newSession.RefreshTokenExpiresAtUtc));
    }
}
