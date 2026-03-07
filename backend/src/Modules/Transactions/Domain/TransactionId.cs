namespace MoneyTracker.Modules.Transactions.Domain;

public readonly record struct TransactionId(Guid Value)
{
    public static TransactionId New() => new(Guid.NewGuid());
}
