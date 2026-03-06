namespace MoneyTracker.Modules.Auth.Domain;

public sealed class AuthDomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
