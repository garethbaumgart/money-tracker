using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Infrastructure;

public sealed class SubscriptionEntitlementServiceTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
    private static readonly Guid HouseholdId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_NoSubscription_ReturnsFreeWithNoFeatures()
    {
        var repository = new StubSubscriptionRepository(subscription: null);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Free, result.Tier);
        Assert.Empty(result.FeatureKeys);
        Assert.Null(result.TrialExpiresAtUtc);
        Assert.Null(result.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_ActiveSubscription_ReturnsPremiumWithAllFeatures()
    {
        var subscription = CreateActiveSubscription();
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Premium, result.Tier);
        Assert.Equal(5, result.FeatureKeys.Count);
        Assert.Contains(FeatureKey.BankSync, result.FeatureKeys);
        Assert.Contains(FeatureKey.PremiumInsights, result.FeatureKeys);
        Assert.Contains(FeatureKey.UnlimitedBudgets, result.FeatureKeys);
        Assert.Contains(FeatureKey.UnlimitedBillReminders, result.FeatureKeys);
        Assert.Contains(FeatureKey.ExportData, result.FeatureKeys);
        Assert.Null(result.TrialExpiresAtUtc);
        Assert.NotNull(result.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_TrialSubscription_ReturnsTrialWithAllFeaturesAndExpiry()
    {
        var trialEnd = NowUtc.AddDays(14);
        var subscription = Subscription.CreateTrial(
            HouseholdId,
            "app-user-1",
            "premium_monthly",
            NowUtc,
            trialEnd,
            NowUtc);
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Trial, result.Tier);
        Assert.Equal(5, result.FeatureKeys.Count);
        Assert.Equal(trialEnd, result.TrialExpiresAtUtc);
        Assert.Equal(trialEnd, result.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_ExpiredSubscription_ReturnsFree()
    {
        var subscription = CreateActiveSubscription();
        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Free, result.Tier);
        Assert.Empty(result.FeatureKeys);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_CancelledSubscription_ReturnsPremium()
    {
        // Cancelled subscriptions remain active until period end
        var subscription = CreateActiveSubscription();
        subscription.Cancel(NowUtc.AddDays(15), "evt-cancel-1", NowUtc.AddDays(15));
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Premium, result.Tier);
        Assert.Equal(5, result.FeatureKeys.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEntitlementsAsync_BillingIssue_ReturnsPremium()
    {
        // Billing issue keeps access until expiration
        var subscription = CreateActiveSubscription();
        subscription.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20));
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.GetEntitlementsAsync(HouseholdId, CancellationToken.None);

        Assert.Equal(SubscriptionTier.Premium, result.Tier);
        Assert.Equal(5, result.FeatureKeys.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IsFeatureAllowedAsync_PremiumWithBankSync_ReturnsTrue()
    {
        var subscription = CreateActiveSubscription();
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.IsFeatureAllowedAsync(HouseholdId, "BankSync", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IsFeatureAllowedAsync_FreeWithBankSync_ReturnsFalse()
    {
        var repository = new StubSubscriptionRepository(subscription: null);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.IsFeatureAllowedAsync(HouseholdId, "BankSync", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task IsFeatureAllowedAsync_InvalidFeatureKey_ReturnsFalse()
    {
        var subscription = CreateActiveSubscription();
        var repository = new StubSubscriptionRepository(subscription);
        var service = new SubscriptionEntitlementService(repository);

        var result = await service.IsFeatureAllowedAsync(HouseholdId, "NonExistentFeature", CancellationToken.None);

        Assert.False(result);
    }

    private static Subscription CreateActiveSubscription()
    {
        var sub = Subscription.CreateTrial(
            HouseholdId,
            "app-user-1",
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        return sub;
    }

    private sealed class StubSubscriptionRepository(Subscription? subscription) : ISubscriptionRepository
    {
        public Task AddAsync(Subscription subscription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken)
            => Task.FromResult(subscription);

        public Task<Subscription?> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken)
            => Task.FromResult(subscription);

        public Task<Subscription?> GetByRevenueCatAppUserIdAsync(string appUserId, CancellationToken cancellationToken)
            => Task.FromResult(subscription);

        public Task<IReadOnlyList<Subscription>> GetExpiredTrialsAsync(DateTimeOffset asOfUtc, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Subscription>>(Array.Empty<Subscription>());
    }
}
