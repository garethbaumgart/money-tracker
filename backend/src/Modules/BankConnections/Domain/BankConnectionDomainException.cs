namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class BankConnectionDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
