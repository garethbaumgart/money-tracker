using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Infrastructure;

// PostgreSQL index recommendations:
// - IX_auth_challenges_token: UNIQUE index on auth_challenges(token) for challenge lookups
// - IX_auth_sessions_access_token: UNIQUE index on auth_sessions(access_token) for token validation
// - IX_auth_sessions_refresh_token: UNIQUE index on auth_sessions(refresh_token) for token refresh
// - IX_auth_users_email: UNIQUE index on auth_users(LOWER(email)) for case-insensitive email lookups
// - IX_auth_users_id: PRIMARY KEY on auth_users(id)
public sealed class InMemoryAuthRepository : IAuthRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<string, AuthChallenge> _challenges = new();
    private readonly Dictionary<string, AuthSession> _sessionsByAccessToken = new();
    private readonly Dictionary<string, AuthSession> _sessionsByRefreshToken = new();
    private readonly Dictionary<string, AuthUser> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, AuthUser> _usersById = new();
    private readonly HashSet<Guid> _deletedUserIds = new();
    private readonly Dictionary<Guid, DateTimeOffset> _scheduledPurges = new();

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

    public Task<AuthUser?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<AuthUser?>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<AuthUser?>(_usersById.GetValueOrDefault(userId));
        }
    }

    public Task MarkUserDeletedAsync(Guid userId, DateTimeOffset scheduledPurgeAtUtc, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _deletedUserIds.Add(userId);
            _scheduledPurges[userId] = scheduledPurgeAtUtc;
        }

        return Task.CompletedTask;
    }
}
