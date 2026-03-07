using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Subscriptions.Application.RestorePurchases;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class RestorePurchasesHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private const string AppUserId = "rc-app-user-1";

    private sealed class StubHouseholdAccessService : IHouseholdAccessService
    {
        private readonly HouseholdAccessResult _result;
        public StubHouseholdAccessService(HouseholdAccessResult result) => _result = result;

        public Task<HouseholdAccessResult> CheckMemberAsync(Guid householdId, Guid userId, CancellationToken ct)
            => Task.FromResult(_result);

        public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }

    private static RestorePurchasesHandler CreateHandler(
        ISubscriptionRepository? repository = null,
        IRevenueCatClient? revenueCatClient = null,
        IHouseholdAccessService? householdAccess = null)
    {
        var repo = repository ?? new InMemorySubscriptionRepository();
        return new RestorePurchasesHandler(
            repo,
            revenueCatClient ?? new InMemoryRevenueCatClient(),
            householdAccess ?? new StubHouseholdAccessService(HouseholdAccessResult.Allowed()),
            new SubscriptionEntitlementService(repo),
            NullLogger<RestorePurchasesHandler>.Instance);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_HouseholdNotFound_ReturnsHouseholdNotFound()
    {
        var handler = CreateHandler(
            householdAccess: new StubHouseholdAccessService(HouseholdAccessResult.NotFound()));

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.HouseholdNotFound, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_AccessDenied_ReturnsAccessDenied()
    {
        // AC-8: Returns 403 for non-member
        var handler = CreateHandler(
            householdAccess: new StubHouseholdAccessService(HouseholdAccessResult.Denied()));

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.AccessDenied, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_NoProviderSubscription_ReturnsFreeState()
    {
        // No subscription in RevenueCat, no local subscription
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("None", result.Status);
        Assert.Equal("Free", result.Tier);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ActiveProviderSubscription_RestoresToActive()
    {
        // AC-5, AC-6: Calls RevenueCat and updates local state
        var repository = new InMemorySubscriptionRepository();
        var revenueCatClient = new InMemoryRevenueCatClient();
        revenueCatClient.SetSubscriber(AppUserId, new SubscriberInfo(
            SubscriptionStatus.Active,
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(30)));

        // Pre-create a trial subscription
        var trial = Subscription.CreateTrial(
            HouseholdId, AppUserId, "trial_premium",
            NowUtc, NowUtc.AddDays(14), NowUtc);
        await repository.AddAsync(trial, CancellationToken.None);

        var handler = CreateHandler(
            repository: repository,
            revenueCatClient: revenueCatClient);

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Premium", result.Tier);
        Assert.Equal(NowUtc.AddDays(30), result.CurrentPeriodEndUtc);

        // Verify local record was updated
        var subscription = await repository.GetByHouseholdIdAsync(HouseholdId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subscription!.Status);
        Assert.Equal("premium_monthly", subscription.ProductId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ProviderError_ReturnsProviderError()
    {
        var failingClient = new FailingRevenueCatClient();
        var handler = CreateHandler(revenueCatClient: failingClient);

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.ProviderError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_NoLocalSubscription_CreatesNewFromProvider()
    {
        // AC-6: Creates new local record when none exists but provider has subscription
        var repository = new InMemorySubscriptionRepository();
        var revenueCatClient = new InMemoryRevenueCatClient();
        revenueCatClient.SetSubscriber(AppUserId, new SubscriberInfo(
            SubscriptionStatus.Active,
            "premium_annual",
            NowUtc,
            NowUtc.AddDays(365)));

        var handler = CreateHandler(
            repository: repository,
            revenueCatClient: revenueCatClient);

        var result = await handler.HandleAsync(
            new RestorePurchasesCommand(HouseholdId, UserId, AppUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Status);

        var subscription = await repository.GetByHouseholdIdAsync(HouseholdId, CancellationToken.None);
        Assert.NotNull(subscription);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("premium_annual", subscription.ProductId);
    }

    private sealed class FailingRevenueCatClient : IRevenueCatClient
    {
        public Task<SubscriberInfo?> GetSubscriberAsync(string appUserId, CancellationToken cancellationToken)
            => throw new HttpRequestException("Connection refused");
    }
}
