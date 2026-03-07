using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Infrastructure;

public sealed class InMemorySubscriptionRepositoryTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddAndGetByHouseholdId_RoundTrip()
    {
        // P4-1-INT-03: Repository persists and retrieves subscription by household
        var repository = new InMemorySubscriptionRepository();
        var householdId = Guid.NewGuid();
        var subscription = Subscription.CreateTrial(
            householdId,
            "app-user-1",
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);

        await repository.AddAsync(subscription, CancellationToken.None);
        var retrieved = await repository.GetByHouseholdIdAsync(householdId, CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(subscription.Id, retrieved.Id);
        Assert.Equal(householdId, retrieved.HouseholdId);
        Assert.Equal("app-user-1", retrieved.RevenueCatAppUserId);
        Assert.Equal(SubscriptionStatus.Trial, retrieved.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByRevenueCatAppUserId_ReturnsCorrectSubscription()
    {
        var repository = new InMemorySubscriptionRepository();
        var subscription = Subscription.CreateTrial(
            Guid.NewGuid(),
            "app-user-unique",
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);

        await repository.AddAsync(subscription, CancellationToken.None);
        var retrieved = await repository.GetByRevenueCatAppUserIdAsync("app-user-unique", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(subscription.Id, retrieved.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByHouseholdId_ReturnsNull_WhenNotFound()
    {
        var repository = new InMemorySubscriptionRepository();

        var retrieved = await repository.GetByHouseholdIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByRevenueCatAppUserId_ReturnsNull_WhenNotFound()
    {
        var repository = new InMemorySubscriptionRepository();

        var retrieved = await repository.GetByRevenueCatAppUserIdAsync("nonexistent", CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetById_ReturnsCorrectSubscription()
    {
        var repository = new InMemorySubscriptionRepository();
        var subscription = Subscription.CreateTrial(
            Guid.NewGuid(),
            "app-user-1",
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);

        await repository.AddAsync(subscription, CancellationToken.None);
        var retrieved = await repository.GetByIdAsync(subscription.Id, CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(subscription.Id, retrieved.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Update_PersistsChanges()
    {
        var repository = new InMemorySubscriptionRepository();
        var householdId = Guid.NewGuid();
        var subscription = Subscription.CreateTrial(
            householdId,
            "app-user-1",
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);

        await repository.AddAsync(subscription, CancellationToken.None);

        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        await repository.UpdateAsync(subscription, CancellationToken.None);

        var retrieved = await repository.GetByHouseholdIdAsync(householdId, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(SubscriptionStatus.Active, retrieved.Status);
    }
}
