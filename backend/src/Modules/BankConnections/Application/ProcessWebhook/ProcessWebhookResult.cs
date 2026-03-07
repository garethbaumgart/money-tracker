using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.ProcessWebhook;

public sealed class ProcessWebhookResult
{
    private ProcessWebhookResult(
        bool isAccepted,
        string? errorCode,
        string? errorMessage)
    {
        IsAccepted = isAccepted;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsAccepted { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static ProcessWebhookResult Accepted()
    {
        return new ProcessWebhookResult(true, errorCode: null, errorMessage: null);
    }

    public static ProcessWebhookResult InvalidSignature()
    {
        return new ProcessWebhookResult(
            false,
            BankConnectionErrors.WebhookInvalidSignature,
            "Invalid webhook signature.");
    }

    public static ProcessWebhookResult InvalidPayload()
    {
        return new ProcessWebhookResult(
            false,
            BankConnectionErrors.WebhookInvalidPayload,
            "Invalid webhook payload.");
    }
}
