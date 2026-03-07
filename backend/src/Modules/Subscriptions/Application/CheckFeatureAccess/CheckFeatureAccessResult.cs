namespace MoneyTracker.Modules.Subscriptions.Application.CheckFeatureAccess;

public sealed class CheckFeatureAccessResult
{
    private CheckFeatureAccessResult(
        bool isAllowed,
        string tier,
        bool upgradeRequired)
    {
        IsAllowed = isAllowed;
        Tier = tier;
        UpgradeRequired = upgradeRequired;
    }

    public bool IsAllowed { get; }
    public string Tier { get; }
    public bool UpgradeRequired { get; }

    public static CheckFeatureAccessResult Allowed(string tier)
    {
        return new CheckFeatureAccessResult(
            isAllowed: true,
            tier: tier,
            upgradeRequired: false);
    }

    public static CheckFeatureAccessResult Denied(string tier)
    {
        return new CheckFeatureAccessResult(
            isAllowed: false,
            tier: tier,
            upgradeRequired: true);
    }
}
