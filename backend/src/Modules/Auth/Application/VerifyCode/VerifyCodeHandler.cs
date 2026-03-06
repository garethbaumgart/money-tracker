using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.VerifyCode;

public sealed class VerifyCodeHandler(IAuthRepository repository, TimeProvider timeProvider)
{
    public async Task<VerifyCodeResult> HandleAsync(VerifyCodeCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = AuthUser.NormalizeEmail(command.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return VerifyCodeResult.Failure(AuthErrors.ValidationError, "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ChallengeToken))
        {
            return VerifyCodeResult.Failure(AuthErrors.ValidationError, "Challenge token is required.");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var challenge = await repository.GetChallengeAsync(command.ChallengeToken, cancellationToken);
        if (challenge is null)
        {
            return VerifyCodeResult.Failure(AuthErrors.ChallengeNotFound, "Challenge token not found.");
        }

        if (challenge.IsUsed)
        {
            return VerifyCodeResult.Failure(AuthErrors.ChallengeUsed, "Challenge token already used.");
        }

        if (challenge.IsExpired(nowUtc))
        {
            return VerifyCodeResult.Failure(AuthErrors.ChallengeExpired, "Challenge token expired.");
        }

        if (challenge.IsChallengeLocked())
        {
            return VerifyCodeResult.Failure(AuthErrors.ChallengeAttemptLimitReached, "Too many failed verification attempts.");
        }

        if (!challenge.IsMatchEmail(normalizedEmail))
        {
            challenge.RecordFailedAttempt();
            await repository.AddChallengeAsync(challenge, cancellationToken);

            return VerifyCodeResult.Failure(
                AuthErrors.ChallengeEmailMismatch,
                "Challenge email does not match the requested email.");
        }

        challenge.MarkUsed();
        await repository.AddChallengeAsync(challenge, cancellationToken);

        var user = await repository.GetOrCreateUserAsync(normalizedEmail, cancellationToken);
        var issuedAtUtc = nowUtc;
        var accessToken = GenerateToken();
        var refreshToken = GenerateToken();

        var session = new AuthSession(
            user.Id,
            user.Email,
            accessToken,
            issuedAtUtc.Add(AuthPolicy.AccessTokenLifetime),
            refreshToken,
            issuedAtUtc.Add(AuthPolicy.RefreshTokenLifetime),
            issuedAtUtc);

        await repository.AddSessionAsync(session, cancellationToken);

        return VerifyCodeResult.Success(new AuthTokenSet(
            user.Id,
            user.Email,
            accessToken,
            session.AccessTokenExpiresAtUtc,
            refreshToken,
            session.RefreshTokenExpiresAtUtc));
    }

    private static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N");
    }
}
