using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.RequestAuthCode;

public sealed class RequestAuthCodeHandler(IAuthRepository repository, TimeProvider timeProvider)
{
    public async Task<RequestAuthCodeResult> HandleAsync(RequestAuthCodeCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = AuthUser.NormalizeEmail(command.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return RequestAuthCodeResult.Validation("Email is required.");
        }

        var challenge = new AuthChallenge(
            Guid.NewGuid().ToString("N"),
            normalizedEmail,
            timeProvider.GetUtcNow().Add(AuthPolicy.ChallengeLifetime));

        await repository.AddChallengeAsync(challenge, cancellationToken);

        return RequestAuthCodeResult.Success(challenge);
    }
}
