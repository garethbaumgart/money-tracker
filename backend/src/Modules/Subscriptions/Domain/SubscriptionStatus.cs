namespace MoneyTracker.Modules.Subscriptions.Domain;

public enum SubscriptionStatus
{
    None,
    Trial,
    Active,
    Cancelled,
    Expired,
    BillingIssue,
    Revoked
}
