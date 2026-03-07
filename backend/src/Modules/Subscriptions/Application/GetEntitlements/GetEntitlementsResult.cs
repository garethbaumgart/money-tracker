using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.GetEntitlements;

public sealed class GetEntitlementsResult
{
    private GetEntitlementsResult(
        string? tier,
        string[]? featureKeys,
        DateTimeOffset? trialExpiresAtUtc,
        DateTimeOffset? currentPeriodEndUtc,
        string? errorCode,
        string? errorMessage)
    {
        Tier = tier;
        FeatureKeys = featureKeys;
        TrialExpiresAtUtc = trialExpiresAtUtc;
        CurrentPeriodEndUtc = currentPeriodEndUtc;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? Tier { get; }
    public string[]? FeatureKeys { get; }
    public DateTimeOffset? TrialExpiresAtUtc { get; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess => ErrorCode is null;

    public static GetEntitlementsResult Success(
        EntitlementResult entitlement)
    {
        return new GetEntitlementsResult(
            entitlement.Tier.ToString(),
            entitlement.FeatureKeys.Select(fk => fk.ToString()).ToArray(),
            entitlement.TrialExpiresAtUtc,
            entitlement.CurrentPeriodEndUtc,
            errorCode: null,
            errorMessage: null);
    }

    public static GetEntitlementsResult HouseholdNotFound()
    {
        return new GetEntitlementsResult(
            tier: null,
            featureKeys: null,
            trialExpiresAtUtc: null,
            currentPeriodEndUtc: null,
            SubscriptionErrors.HouseholdNotFound,
            "The household was not found.");
    }

    public static GetEntitlementsResult AccessDenied()
    {
        return new GetEntitlementsResult(
            tier: null,
            featureKeys: null,
            trialExpiresAtUtc: null,
            currentPeriodEndUtc: null,
            SubscriptionErrors.AccessDenied,
            "You do not have access to this household.");
    }
}
