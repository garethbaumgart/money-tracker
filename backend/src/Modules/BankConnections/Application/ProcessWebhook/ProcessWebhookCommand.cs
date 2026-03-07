namespace MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;

public sealed record ProcessWebhookCommand(
    string Signature,
    string RawBody,
    string? EventType,
    string? ConnectionId);
