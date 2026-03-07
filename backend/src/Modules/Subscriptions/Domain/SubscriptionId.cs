namespace MoneyTracker.Modules.Subscriptions.Domain;

public readonly record struct SubscriptionId(Guid Value)
{
    public static SubscriptionId New() => new(Guid.NewGuid());
}
