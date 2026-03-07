using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Subscriptions.Application.GetEntitlements;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class GetEntitlementsHandlerTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ActiveSubscription_ReturnsPremiumTierWithAllFeatures()
    {
        // P4-2-UNIT-06: GetEntitlements with Active subscription returns Premium tier
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Premium,
                EntitlementSet.ForTier(SubscriptionTier.Premium).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: DateTimeOffset.UtcNow.AddDays(30)));

        var householdAccess = new StubHouseholdAccessService(HouseholdAccessResult.Allowed());

        var handler = new GetEntitlementsHandler(entitlementService, householdAccess);
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(HouseholdId, UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Premium", result.Tier);
        Assert.NotNull(result.FeatureKeys);
        Assert.Contains("BankSync", result.FeatureKeys!);
        Assert.Contains("PremiumInsights", result.FeatureKeys!);
        Assert.Contains("UnlimitedBudgets", result.FeatureKeys!);
        Assert.Contains("UnlimitedBillReminders", result.FeatureKeys!);
        Assert.Contains("ExportData", result.FeatureKeys!);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_NoSubscription_ReturnsFreeTier()
    {
        // P4-2-UNIT-07: GetEntitlements with no subscription returns Free tier
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Free,
                EntitlementSet.ForTier(SubscriptionTier.Free).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null));

        var householdAccess = new StubHouseholdAccessService(HouseholdAccessResult.Allowed());

        var handler = new GetEntitlementsHandler(entitlementService, householdAccess);
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(HouseholdId, UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Free", result.Tier);
        Assert.NotNull(result.FeatureKeys);
        Assert.Empty(result.FeatureKeys!);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_TrialSubscription_ReturnsTrialTierWithAllFeatures()
    {
        var trialExpiry = DateTimeOffset.UtcNow.AddDays(14);
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Trial,
                EntitlementSet.ForTier(SubscriptionTier.Trial).FeatureKeys,
                TrialExpiresAtUtc: trialExpiry,
                CurrentPeriodEndUtc: trialExpiry));

        var householdAccess = new StubHouseholdAccessService(HouseholdAccessResult.Allowed());

        var handler = new GetEntitlementsHandler(entitlementService, householdAccess);
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(HouseholdId, UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Trial", result.Tier);
        Assert.NotNull(result.FeatureKeys);
        Assert.Equal(5, result.FeatureKeys!.Length);
        Assert.Equal(trialExpiry, result.TrialExpiresAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_HouseholdNotFound_ReturnsHouseholdNotFound()
    {
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Free,
                EntitlementSet.ForTier(SubscriptionTier.Free).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null));

        var householdAccess = new StubHouseholdAccessService(HouseholdAccessResult.NotFound());

        var handler = new GetEntitlementsHandler(entitlementService, householdAccess);
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(HouseholdId, UserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.HouseholdNotFound, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_AccessDenied_ReturnsAccessDenied()
    {
        var entitlementService = new StubEntitlementService(
            new EntitlementResult(
                SubscriptionTier.Free,
                EntitlementSet.ForTier(SubscriptionTier.Free).FeatureKeys,
                TrialExpiresAtUtc: null,
                CurrentPeriodEndUtc: null));

        var householdAccess = new StubHouseholdAccessService(HouseholdAccessResult.Denied());

        var handler = new GetEntitlementsHandler(entitlementService, householdAccess);
        var result = await handler.HandleAsync(
            new GetEntitlementsQuery(HouseholdId, UserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.AccessDenied, result.ErrorCode);
    }

    private sealed class StubEntitlementService(EntitlementResult result) : ISubscriptionEntitlementService
    {
        public Task<EntitlementResult> GetEntitlementsAsync(Guid householdId, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class StubHouseholdAccessService(HouseholdAccessResult accessResult) : IHouseholdAccessService
    {
        public Task<HouseholdAccessResult> CheckMemberAsync(Guid householdId, Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(accessResult);
        }

        public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
        }
    }
}
