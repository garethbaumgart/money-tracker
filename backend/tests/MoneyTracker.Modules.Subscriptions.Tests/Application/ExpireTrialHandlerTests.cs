using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.Subscriptions.Application.ExpireTrial;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class ExpireTrialHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    private static ExpireTrialHandler CreateHandler(ISubscriptionRepository? repository = null)
    {
        return new ExpireTrialHandler(
            repository ?? new InMemorySubscriptionRepository(),
            NullLogger<ExpireTrialHandler>.Instance);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_NoExpiredTrials_ReturnsZero()
    {
        var repository = new InMemorySubscriptionRepository();
        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ExpireTrialCommand(NowUtc),
            CancellationToken.None);

        Assert.Equal(0, result.ExpiredCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ExpiredTrial_TransitionsToNone()
    {
        // AC-3: TrialExpiryWorker transitions expired trial to None
        var repository = new InMemorySubscriptionRepository();
        var trialEnd = NowUtc.AddDays(14);
        var trial = Subscription.CreateTrial(
            Guid.NewGuid(), "user-1", "trial_premium",
            NowUtc, trialEnd, NowUtc);
        await repository.AddAsync(trial, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        // Run expiry after trial end
        var result = await handler.HandleAsync(
            new ExpireTrialCommand(trialEnd.AddHours(1)),
            CancellationToken.None);

        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(SubscriptionStatus.None, trial.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_MultipleExpiredTrials_TransitionsAll()
    {
        var repository = new InMemorySubscriptionRepository();

        // Create 3 expired trials
        for (int i = 0; i < 3; i++)
        {
            var trial = Subscription.CreateTrial(
                Guid.NewGuid(), $"user-{i}", "trial_premium",
                NowUtc, NowUtc.AddDays(14), NowUtc);
            await repository.AddAsync(trial, CancellationToken.None);
        }

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ExpireTrialCommand(NowUtc.AddDays(15)),
            CancellationToken.None);

        Assert.Equal(3, result.ExpiredCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_MixOfExpiredAndActive_OnlyExpiresTrials()
    {
        // AC-4: Trial expiry does not affect Active, Cancelled, or BillingIssue subscriptions
        var repository = new InMemorySubscriptionRepository();

        // Expired trial
        var expiredTrial = Subscription.CreateTrial(
            Guid.NewGuid(), "user-expired", "trial_premium",
            NowUtc, NowUtc.AddDays(14), NowUtc);
        await repository.AddAsync(expiredTrial, CancellationToken.None);

        // Active trial (not yet expired)
        var activeTrial = Subscription.CreateTrial(
            Guid.NewGuid(), "user-active-trial", "trial_premium",
            NowUtc, NowUtc.AddDays(30), NowUtc);
        await repository.AddAsync(activeTrial, CancellationToken.None);

        // Active subscription
        var active = Subscription.CreateTrial(
            Guid.NewGuid(), "user-active", "premium_monthly",
            NowUtc, NowUtc.AddDays(7), NowUtc);
        active.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        await repository.AddAsync(active, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ExpireTrialCommand(NowUtc.AddDays(15)),
            CancellationToken.None);

        // Only 1 expired trial should be transitioned
        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(SubscriptionStatus.None, expiredTrial.Status);
        Assert.Equal(SubscriptionStatus.Trial, activeTrial.Status);
        Assert.Equal(SubscriptionStatus.Active, active.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ExactlyAtExpiryTime_ExpiresTheTrial()
    {
        var repository = new InMemorySubscriptionRepository();
        var trialEnd = NowUtc.AddDays(14);
        var trial = Subscription.CreateTrial(
            Guid.NewGuid(), "user-exact", "trial_premium",
            NowUtc, trialEnd, NowUtc);
        await repository.AddAsync(trial, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        // Run expiry at exactly the trial end time
        var result = await handler.HandleAsync(
            new ExpireTrialCommand(trialEnd),
            CancellationToken.None);

        Assert.Equal(1, result.ExpiredCount);
        Assert.Equal(SubscriptionStatus.None, trial.Status);
    }
}
