using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;

public sealed class ProcessRevenueCatWebhookResult
{
    private ProcessRevenueCatWebhookResult(
        string? errorCode,
        string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static ProcessRevenueCatWebhookResult Accepted()
    {
        return new ProcessRevenueCatWebhookResult(errorCode: null, errorMessage: null);
    }

    public static ProcessRevenueCatWebhookResult InvalidSignature()
    {
        return new ProcessRevenueCatWebhookResult(
            SubscriptionErrors.WebhookInvalidSignature,
            "Invalid webhook signature.");
    }

    public static ProcessRevenueCatWebhookResult InvalidPayload(string detail)
    {
        return new ProcessRevenueCatWebhookResult(
            SubscriptionErrors.WebhookInvalidPayload,
            detail);
    }
}
