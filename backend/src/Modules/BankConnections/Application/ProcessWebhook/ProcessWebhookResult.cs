using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;

public sealed class ProcessWebhookResult
{
    private ProcessWebhookResult(
        string? errorCode,
        string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static ProcessWebhookResult Accepted()
    {
        return new ProcessWebhookResult(errorCode: null, errorMessage: null);
    }

    public static ProcessWebhookResult InvalidSignature()
    {
        return new ProcessWebhookResult(
            BankConnectionErrors.WebhookInvalidSignature,
            "Invalid webhook signature.");
    }
}
