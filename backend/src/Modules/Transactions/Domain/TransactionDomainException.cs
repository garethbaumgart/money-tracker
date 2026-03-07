namespace MoneyTracker.Modules.Transactions.Domain;

public sealed class TransactionDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
