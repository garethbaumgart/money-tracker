namespace MoneyTracker.Modules.Subscriptions.Domain;

public sealed class SubscriptionDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
