using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.RestorePurchases;

public sealed class RestorePurchasesResult
{
    private RestorePurchasesResult(
        string? status,
        string? tier,
        string[]? featureKeys,
        DateTimeOffset? currentPeriodEndUtc,
        string? errorCode,
        string? errorMessage)
    {
        Status = status;
        Tier = tier;
        FeatureKeys = featureKeys;
        CurrentPeriodEndUtc = currentPeriodEndUtc;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? Status { get; }
    public string? Tier { get; }
    public string[]? FeatureKeys { get; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess => ErrorCode is null;

    public static RestorePurchasesResult Success(
        SubscriptionStatus status,
        SubscriptionTier tier,
        string[] featureKeys,
        DateTimeOffset? currentPeriodEndUtc)
    {
        return new RestorePurchasesResult(
            status.ToString(),
            tier.ToString(),
            featureKeys,
            currentPeriodEndUtc,
            errorCode: null,
            errorMessage: null);
    }

    public static RestorePurchasesResult HouseholdNotFound()
    {
        return new RestorePurchasesResult(
            null, null, null, null,
            SubscriptionErrors.HouseholdNotFound,
            "The household was not found.");
    }

    public static RestorePurchasesResult AccessDenied()
    {
        return new RestorePurchasesResult(
            null, null, null, null,
            SubscriptionErrors.AccessDenied,
            "You do not have access to this household.");
    }

    public static RestorePurchasesResult ProviderError(string message)
    {
        return new RestorePurchasesResult(
            null, null, null, null,
            SubscriptionErrors.ProviderError,
            message);
    }

    public static RestorePurchasesResult RestoreFailed(string message)
    {
        return new RestorePurchasesResult(
            null, null, null, null,
            SubscriptionErrors.RestoreFailed,
            message);
    }
}
