namespace MoneyTracker.Modules.BankConnections.Domain;

public readonly record struct LinkEventId(Guid Value)
{
    public static LinkEventId New() => new(Guid.NewGuid());
}
