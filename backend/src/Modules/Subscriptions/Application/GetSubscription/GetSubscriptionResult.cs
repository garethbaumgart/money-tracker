using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.GetSubscription;

public sealed class GetSubscriptionResult
{
    private GetSubscriptionResult(
        SubscriptionSummary? subscription,
        string? errorCode,
        string? errorMessage)
    {
        Subscription = subscription;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public SubscriptionSummary? Subscription { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static GetSubscriptionResult Success(SubscriptionSummary subscription)
    {
        return new GetSubscriptionResult(subscription, errorCode: null, errorMessage: null);
    }

    public static GetSubscriptionResult NotFound()
    {
        return new GetSubscriptionResult(
            subscription: null,
            SubscriptionErrors.SubscriptionNotFound,
            "No subscription found for this household.");
    }
}

public sealed record SubscriptionSummary(
    Guid Id,
    Guid HouseholdId,
    string RevenueCatAppUserId,
    string ProductId,
    string Status,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? BillingIssueDetectedAtUtc,
    DateTimeOffset? OriginalPurchaseDateUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
