namespace MoneyTracker.Modules.Subscriptions.Domain;

public interface IRevenueCatWebhookSignatureValidator
{
    bool Validate(string signature, string rawBody);
}
