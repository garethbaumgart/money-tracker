namespace MoneyTracker.Modules.Subscriptions.Domain;

public static class SubscriptionErrors
{
    public const string ValidationError = "subscription_validation_error";
    public const string SubscriptionNotFound = "subscription_not_found";
    public const string InvalidStateTransition = "subscription_invalid_state_transition";
    public const string WebhookInvalidSignature = "subscription_webhook_invalid_signature";
    public const string WebhookInvalidPayload = "subscription_webhook_invalid_payload";
    public const string HouseholdNotFound = "subscription_household_not_found";
    public const string AccessDenied = "subscription_access_denied";
    public const string TrialAlreadyStarted = "subscription_trial_already_started";
    public const string SubscriptionAlreadyActive = "subscription_already_active";
    public const string RestoreFailed = "subscription_restore_failed";
    public const string ProviderError = "subscription_provider_error";
}
