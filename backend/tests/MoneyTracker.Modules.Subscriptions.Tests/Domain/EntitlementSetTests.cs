using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Tests.Domain;

public sealed class EntitlementSetTests
{
    private static readonly FeatureKey[] AllPremiumFeatures =
    [
        FeatureKey.BankSync,
        FeatureKey.PremiumInsights,
        FeatureKey.UnlimitedBudgets,
        FeatureKey.UnlimitedBillReminders,
        FeatureKey.ExportData
    ];

    [Fact]
    [Trait("Category", "Unit")]
    public void ForTier_Premium_ContainsAllPremiumFeatureKeys()
    {
        // P4-2-UNIT-01: EntitlementSet.ForTier(Premium) contains all 5 premium feature keys
        var entitlementSet = EntitlementSet.ForTier(SubscriptionTier.Premium);

        Assert.Equal(SubscriptionTier.Premium, entitlementSet.Tier);
        Assert.Equal(AllPremiumFeatures.Length, entitlementSet.FeatureKeys.Count);
        foreach (var feature in AllPremiumFeatures)
        {
            Assert.True(entitlementSet.HasFeature(feature),
                $"Premium tier should include {feature}");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ForTier_Free_ContainsNoPremiumFeatureKeys()
    {
        // P4-2-UNIT-02: EntitlementSet.ForTier(Free) contains no premium feature keys
        var entitlementSet = EntitlementSet.ForTier(SubscriptionTier.Free);

        Assert.Equal(SubscriptionTier.Free, entitlementSet.Tier);
        Assert.Empty(entitlementSet.FeatureKeys);
        foreach (var feature in AllPremiumFeatures)
        {
            Assert.False(entitlementSet.HasFeature(feature),
                $"Free tier should not include {feature}");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ForTier_Trial_ContainsAllPremiumFeatureKeys()
    {
        // P4-2-UNIT-03: EntitlementSet.ForTier(Trial) contains all premium feature keys (same as Premium)
        var entitlementSet = EntitlementSet.ForTier(SubscriptionTier.Trial);

        Assert.Equal(SubscriptionTier.Trial, entitlementSet.Tier);
        Assert.Equal(AllPremiumFeatures.Length, entitlementSet.FeatureKeys.Count);
        foreach (var feature in AllPremiumFeatures)
        {
            Assert.True(entitlementSet.HasFeature(feature),
                $"Trial tier should include {feature}");
        }
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(FeatureKey.BankSync)]
    [InlineData(FeatureKey.PremiumInsights)]
    [InlineData(FeatureKey.UnlimitedBudgets)]
    [InlineData(FeatureKey.UnlimitedBillReminders)]
    [InlineData(FeatureKey.ExportData)]
    public void HasFeature_Premium_ReturnsTrueForEachFeature(FeatureKey feature)
    {
        var entitlementSet = EntitlementSet.ForTier(SubscriptionTier.Premium);

        Assert.True(entitlementSet.HasFeature(feature));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(FeatureKey.BankSync)]
    [InlineData(FeatureKey.PremiumInsights)]
    [InlineData(FeatureKey.UnlimitedBudgets)]
    [InlineData(FeatureKey.UnlimitedBillReminders)]
    [InlineData(FeatureKey.ExportData)]
    public void HasFeature_Free_ReturnsFalseForEachFeature(FeatureKey feature)
    {
        var entitlementSet = EntitlementSet.ForTier(SubscriptionTier.Free);

        Assert.False(entitlementSet.HasFeature(feature));
    }
}
