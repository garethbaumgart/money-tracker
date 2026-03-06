using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Infrastructure;

public sealed class InMemoryAuthRepository : IAuthRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, AuthChallenge> _challenges = new();
    private readonly Dictionary<string, AuthSession> _sessionsByAccessToken = new();
    private readonly Dictionary<string, AuthSession> _sessionsByRefreshToken = new();
    private readonly Dictionary<string, AuthUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, AuthUser> _usersById = new();

    public Task AddChallengeAsync(AuthChallenge challenge, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _challenges[challenge.Token] = challenge;
        }

        return Task.CompletedTask;
    }

    public Task<AuthChallenge?> GetChallengeAsync(string challengeToken, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AuthChallenge?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<AuthChallenge?>(_challenges.GetValueOrDefault(challengeToken));
        }
    }

    public Task<AuthUser> GetOrCreateUserAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AuthUser>(cancellationToken);
        }

        lock (_sync)
        {
            if (_usersByEmail.TryGetValue(normalizedEmail, out var existingUser))
            {
                return Task.FromResult(existingUser);
            }

            var createdUser = new AuthUser(Guid.NewGuid(), normalizedEmail);
            _usersByEmail[createdUser.Email] = createdUser;
            _usersById[createdUser.Id] = createdUser;
            return Task.FromResult(createdUser);
        }
    }

    public Task AddSessionAsync(AuthSession session, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _sessionsByAccessToken[session.AccessToken] = session;
            _sessionsByRefreshToken[session.RefreshToken] = session;
        }

        return Task.CompletedTask;
    }

    public Task<AuthSession?> GetSessionByAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AuthSession?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<AuthSession?>(_sessionsByAccessToken.GetValueOrDefault(accessToken));
        }
    }

    public Task<AuthSession?> GetSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AuthSession?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<AuthSession?>(_sessionsByRefreshToken.GetValueOrDefault(refreshToken));
        }
    }

    public Task ReplaceSessionAsync(
        string oldRefreshToken,
        string oldAccessToken,
        AuthSession newSession,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _sessionsByAccessToken.Remove(oldAccessToken);
            _sessionsByRefreshToken.Remove(oldRefreshToken);

            _sessionsByAccessToken[newSession.AccessToken] = newSession;
            _sessionsByRefreshToken[newSession.RefreshToken] = newSession;
        }

        return Task.CompletedTask;
    }

    public Task RemoveSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            if (_sessionsByRefreshToken.Remove(refreshToken, out var existingSession))
            {
                _sessionsByAccessToken.Remove(existingSession.AccessToken);
            }
        }

        return Task.CompletedTask;
    }
}
