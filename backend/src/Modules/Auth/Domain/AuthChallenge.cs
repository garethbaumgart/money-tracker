namespace MoneyTracker.Modules.Auth.Domain;

public sealed class AuthChallenge
{
    public AuthChallenge(string token, string email, DateTimeOffset expiresAtUtc)
    {
        Token = token;
        Email = email;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Token { get; }
    public string Email { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public int FailedAttempts { get; private set; }
    public bool IsUsed { get; private set; }

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        return nowUtc >= ExpiresAtUtc;
    }

    public bool IsMatchEmail(string normalizedEmail)
    {
        return string.Equals(Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsChallengeLocked()
    {
        return FailedAttempts >= AuthPolicy.MaxChallengeAttempts;
    }

    public void RecordFailedAttempt()
    {
        FailedAttempts++;
    }

    public void MarkUsed()
    {
        IsUsed = true;
    }
}
