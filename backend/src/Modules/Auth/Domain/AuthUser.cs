namespace MoneyTracker.Modules.Auth.Domain;

public sealed class AuthUser
{
    public AuthUser(Guid id, string email)
    {
        Id = id;
        Email = email;
    }

    public Guid Id { get; }
    public string Email { get; }

    public static AuthUser Create(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new AuthDomainException(AuthErrors.ValidationError, "Email is required.");
        }

        return new AuthUser(Guid.NewGuid(), normalizedEmail);
    }

    public static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
