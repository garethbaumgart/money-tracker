namespace MoneyTracker.Modules.BankConnections.Domain;

public readonly record struct SyncEventId(Guid Value)
{
    public static SyncEventId New() => new(Guid.NewGuid());
}
