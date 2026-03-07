using MoneyTracker.Modules.Subscriptions.Application.CheckFeatureAccess;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class CheckFeatureAccessHandlerTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_PremiumTier_BankSync_ReturnsAllowed()
    {
        // P4-2-UNIT-04: CheckFeatureAccess with Premium tier and BankSync returns IsAllowed=true
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Premium,
                EntitlementSet.ForTier(SubscriptionTier.Premium).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: DateTimeOffset.UtcNow.AddDays(30)));

        var handler = new CheckFeatureAccessHandler(entitlementService);
        var result = await handler.HandleAsync(
            new CheckFeatureAccessQuery(HouseholdId, FeatureKey.BankSync),
            CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.Equal("Premium", result.Tier);
        Assert.False(result.UpgradeRequired);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_FreeTier_BankSync_ReturnsDenied()
    {
        // P4-2-UNIT-05: CheckFeatureAccess with Free tier and BankSync returns IsAllowed=false, UpgradeRequired=true
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Free,
                EntitlementSet.ForTier(SubscriptionTier.Free).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null));

        var handler = new CheckFeatureAccessHandler(entitlementService);
        var result = await handler.HandleAsync(
            new CheckFeatureAccessQuery(HouseholdId, FeatureKey.BankSync),
            CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Equal("Free", result.Tier);
        Assert.True(result.UpgradeRequired);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_TrialTier_PremiumInsights_ReturnsAllowed()
    {
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Trial,
                EntitlementSet.ForTier(SubscriptionTier.Trial).FeatureKeys,
                TrialExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(14),
                CurrentPeriodEndUtc: DateTimeOffset.UtcNow.AddDays(14)));

        var handler = new CheckFeatureAccessHandler(entitlementService);
        var result = await handler.HandleAsync(
            new CheckFeatureAccessQuery(HouseholdId, FeatureKey.PremiumInsights),
            CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.Equal("Trial", result.Tier);
        Assert.False(result.UpgradeRequired);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(FeatureKey.BankSync)]
    [InlineData(FeatureKey.PremiumInsights)]
    [InlineData(FeatureKey.UnlimitedBudgets)]
    [InlineData(FeatureKey.UnlimitedBillReminders)]
    [InlineData(FeatureKey.ExportData)]
    public async Task HandleAsync_FreeTier_AllPremiumFeatures_ReturnsDenied(FeatureKey feature)
    {
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Free,
                EntitlementSet.ForTier(SubscriptionTier.Free).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null));

        var handler = new CheckFeatureAccessHandler(entitlementService);
        var result = await handler.HandleAsync(
            new CheckFeatureAccessQuery(HouseholdId, feature),
            CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.True(result.UpgradeRequired);
    }

    private sealed class StubEntitlementService(EntitlementResult result) : ISubscriptionEntitlementService
    {
        public Task<EntitlementResult> GetEntitlementsAsync(Guid householdId, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
