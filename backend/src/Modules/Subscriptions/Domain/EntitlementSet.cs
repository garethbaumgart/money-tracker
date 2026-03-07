namespace MoneyTracker.Modules.Subscriptions.Domain;

public sealed class EntitlementSet
{
    private static readonly IReadOnlySet<FeatureKey> PremiumFeatures = new HashSet<FeatureKey>
    {
        FeatureKey.BankSync,
        FeatureKey.PremiumInsights,
        FeatureKey.UnlimitedBudgets,
        FeatureKey.UnlimitedBillReminders,
        FeatureKey.ExportData
    };

    private static readonly IReadOnlySet<FeatureKey> FreeFeatures =
        new HashSet<FeatureKey>();

    private EntitlementSet(SubscriptionTier tier, IReadOnlySet<FeatureKey> featureKeys)
    {
        Tier = tier;
        FeatureKeys = featureKeys;
    }

    public SubscriptionTier Tier { get; }

    public IReadOnlySet<FeatureKey> FeatureKeys { get; }

    public bool HasFeature(FeatureKey feature) => FeatureKeys.Contains(feature);

    public static EntitlementSet ForTier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Premium => new EntitlementSet(tier, PremiumFeatures),
        SubscriptionTier.Trial => new EntitlementSet(tier, PremiumFeatures),
        SubscriptionTier.Free => new EntitlementSet(tier, FreeFeatures),
        _ => new EntitlementSet(SubscriptionTier.Free, FreeFeatures)
    };
}
