namespace MoneyTracker.Modules.BankConnections.Domain;

public readonly record struct BankConnectionId(Guid Value)
{
    public static BankConnectionId New() => new(Guid.NewGuid());
}
