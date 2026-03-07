namespace MoneyTracker.Modules.BankConnections.Domain;

public interface IWebhookSignatureValidator
{
    bool Validate(string signature, string rawBody);
}
