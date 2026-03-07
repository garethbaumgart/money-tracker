using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.Subscriptions.Application.StartTrial;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class StartTrialHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static StartTrialHandler CreateHandler(
        ISubscriptionRepository? repository = null,
        TimeProvider? timeProvider = null)
    {
        return new StartTrialHandler(
            repository ?? new InMemorySubscriptionRepository(),
            timeProvider ?? new FakeTimeProvider(NowUtc),
            NullLogger<StartTrialHandler>.Instance);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_NewHousehold_CreatesTrialSubscription()
    {
        // AC-1: Auto-grants 14-day trial on household creation
        var repository = new InMemorySubscriptionRepository();
        var handler = CreateHandler(repository: repository);
        var householdId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new StartTrialCommand(householdId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.TrialExpiresAtUtc);
        Assert.Equal(NowUtc.AddDays(14), result.TrialExpiresAtUtc);

        var subscription = await repository.GetByHouseholdIdAsync(householdId, CancellationToken.None);
        Assert.NotNull(subscription);
        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
        Assert.Equal(NowUtc, subscription.TrialStartedAtUtc);
        Assert.Equal(NowUtc.AddDays(14), subscription.TrialExpiresAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ExistingSubscription_IsIdempotent()
    {
        // AC-2: Idempotent — does not create duplicate or reset trial
        var repository = new InMemorySubscriptionRepository();
        var householdId = Guid.NewGuid();

        // Pre-create a trial subscription
        var existing = Subscription.CreateTrial(
            householdId,
            householdId.ToString(),
            "trial_premium",
            NowUtc,
            NowUtc.AddDays(14),
            NowUtc);
        await repository.AddAsync(existing, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new StartTrialCommand(householdId),
            CancellationToken.None);

        // Should succeed but not create a new subscription
        Assert.True(result.IsSuccess);
        Assert.Null(result.TrialExpiresAtUtc); // AlreadyExists returns null

        // Original subscription should be unchanged
        var subscription = await repository.GetByHouseholdIdAsync(householdId, CancellationToken.None);
        Assert.NotNull(subscription);
        Assert.Equal(existing.Id, subscription.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ExistingActiveSubscription_IsIdempotent()
    {
        // AC-2: Idempotent — does not affect active subscriptions
        var repository = new InMemorySubscriptionRepository();
        var householdId = Guid.NewGuid();

        var existing = Subscription.CreateTrial(
            householdId,
            householdId.ToString(),
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);
        existing.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        await repository.AddAsync(existing, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new StartTrialCommand(householdId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subscription = await repository.GetByHouseholdIdAsync(householdId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subscription!.Status);
    }
}
