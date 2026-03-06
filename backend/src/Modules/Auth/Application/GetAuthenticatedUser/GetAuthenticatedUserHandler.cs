using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;

public sealed class GetAuthenticatedUserHandler(IAuthRepository repository, TimeProvider timeProvider)
{
    public async Task<GetAuthenticatedUserResult> HandleAsync(
        GetAuthenticatedUserQuery query,
        CancellationToken cancellationToken)
    {
        var accessToken = query.AccessToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return GetAuthenticatedUserResult.Failure(AuthErrors.AccessTokenMissing, "Access token is missing.");
        }

        var session = await repository.GetSessionByAccessTokenAsync(accessToken, cancellationToken);
        if (session is null)
        {
            return GetAuthenticatedUserResult.Failure(AuthErrors.AccessTokenInvalid, "Access token is invalid.");
        }

        var nowUtc = timeProvider.GetUtcNow();
        if (session.AccessTokenExpiresAtUtc < nowUtc)
        {
            return GetAuthenticatedUserResult.Failure(AuthErrors.AccessTokenExpired, "Access token expired.");
        }

        return GetAuthenticatedUserResult.Success(session.UserId, session.UserEmail);
    }
}
