using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.Logout;

public sealed class LogoutSessionHandler(IAuthRepository repository)
{
    public async Task<LogoutSessionResult> HandleAsync(LogoutSessionCommand command, CancellationToken cancellationToken)
    {
        var normalizedToken = command.RefreshToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return LogoutSessionResult.Success();
        }

        await repository.RemoveSessionByRefreshTokenAsync(normalizedToken, cancellationToken);
        return LogoutSessionResult.Success();
    }
}
