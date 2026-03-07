using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyTracker.Modules.Subscriptions.Application.ProcessWebhook;
using MoneyTracker.Modules.Subscriptions.Domain;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Application;

public sealed class ProcessRevenueCatWebhookHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    private sealed class AlwaysValidSignatureValidator : IRevenueCatWebhookSignatureValidator
    {
        public bool Validate(string signature, string rawBody) => true;
    }

    private sealed class AlwaysInvalidSignatureValidator : IRevenueCatWebhookSignatureValidator
    {
        public bool Validate(string signature, string rawBody) => false;
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static ProcessRevenueCatWebhookHandler CreateHandler(
        IRevenueCatWebhookSignatureValidator? signatureValidator = null,
        ISubscriptionRepository? repository = null,
        TimeProvider? timeProvider = null)
    {
        return new ProcessRevenueCatWebhookHandler(
            signatureValidator ?? new AlwaysValidSignatureValidator(),
            repository ?? new InMemorySubscriptionRepository(),
            timeProvider ?? new FakeTimeProvider(NowUtc),
            NullLogger<ProcessRevenueCatWebhookHandler>.Instance);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_InvalidSignature_ReturnsUnauthorized()
    {
        // Webhook with invalid signature returns error
        var handler = CreateHandler(signatureValidator: new AlwaysInvalidSignatureValidator());

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "bad-sig", "{}", "INITIAL_PURCHASE", "user-1", "product-1", "evt-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.WebhookInvalidSignature, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_MissingEventType_ReturnsInvalidPayload()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", null, "user-1", "product-1", "evt-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.WebhookInvalidPayload, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_MissingAppUserId_ReturnsInvalidPayload()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "INITIAL_PURCHASE", null, "product-1", "evt-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.WebhookInvalidPayload, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_InitialPurchase_CreatesActiveSubscription()
    {
        // AC-3: INITIAL_PURCHASE creates subscription in Active status
        var repository = new InMemorySubscriptionRepository();
        var handler = CreateHandler(repository: repository);
        var householdId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "INITIAL_PURCHASE", householdId.ToString(), "premium_monthly", "evt-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subscription = await repository.GetByRevenueCatAppUserIdAsync(
            householdId.ToString(), CancellationToken.None);
        Assert.NotNull(subscription);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("premium_monthly", subscription.ProductId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_InitialPurchase_TransitionsTrialToActive()
    {
        // AC-3: INITIAL_PURCHASE transitions existing Trial subscription to Active
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-trial-1";
        var trial = Subscription.CreateTrial(
            Guid.NewGuid(), appUserId, "premium_monthly",
            NowUtc, NowUtc.AddDays(7), NowUtc);
        await repository.AddAsync(trial, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "INITIAL_PURCHASE", appUserId, "premium_monthly", "evt-purchase-1",
                NowUtc.AddDays(7), NowUtc.AddDays(37), NowUtc, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subscription = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.NotNull(subscription);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("evt-purchase-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_DuplicateEvent_IsIdempotent()
    {
        // AC-9: Duplicate events are acknowledged but not re-processed
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-dup-1";
        var trial = Subscription.CreateTrial(
            Guid.NewGuid(), appUserId, "premium_monthly",
            NowUtc, NowUtc.AddDays(7), NowUtc);
        await repository.AddAsync(trial, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        // First call: activates
        await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "INITIAL_PURCHASE", appUserId, "premium_monthly", "evt-dup-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        var subAfterFirst = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subAfterFirst!.Status);
        var firstUpdatedAt = subAfterFirst.UpdatedAtUtc;

        // Second call: same event ID, should be idempotent
        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "INITIAL_PURCHASE", appUserId, "premium_monthly", "evt-dup-1",
                NowUtc, NowUtc.AddDays(30), NowUtc, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subAfterSecond = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subAfterSecond!.Status);
        // UpdatedAtUtc should not change on duplicate
        Assert.Equal(firstUpdatedAt, subAfterSecond.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Renewal_ExtendsPeriod()
    {
        // AC-4: RENEWAL extends subscription period
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-renew-1";
        var sub = Subscription.CreateTrial(
            Guid.NewGuid(), appUserId, "premium_monthly",
            NowUtc, NowUtc.AddDays(7), NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.UpdateAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "RENEWAL", appUserId, "premium_monthly", "evt-renew-1",
                NowUtc.AddDays(30), NowUtc.AddDays(60), null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, updated!.Status);
        Assert.Equal(NowUtc.AddDays(60), updated.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Cancellation_MarksCancelled()
    {
        // AC-5: CANCELLATION marks subscription as Cancelled
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-cancel-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);
        var cancelDate = NowUtc.AddDays(15);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "CANCELLATION", appUserId, "premium_monthly", "evt-cancel-1",
                null, null, null, cancelDate),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Cancelled, updated!.Status);
        Assert.Equal(cancelDate, updated.CancelledAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Expiration_TransitionsToExpired()
    {
        // AC-6: EXPIRATION transitions to Expired
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-expire-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "EXPIRATION", appUserId, "premium_monthly", "evt-expire-1",
                null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Expired, updated!.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_BillingIssue_TransitionsToBillingIssue()
    {
        // AC-7: BILLING_ISSUE transitions to BillingIssue
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-billing-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "BILLING_ISSUE", appUserId, "premium_monthly", "evt-billing-1",
                null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.BillingIssue, updated!.Status);
        Assert.NotNull(updated.BillingIssueDetectedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_ProductChange_UpdatesProductId()
    {
        // AC-8: PRODUCT_CHANGE updates product ID
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-change-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "PRODUCT_CHANGE", appUserId, "premium_annual", "evt-change-1",
                null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal("premium_annual", updated!.ProductId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Transfer_RevokesSubscription()
    {
        // TRANSFER event revokes subscription
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-transfer-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "TRANSFER", appUserId, null, "evt-transfer-1",
                null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Revoked, updated!.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_UnknownEventType_ReturnsAccepted()
    {
        // Unknown event types are acknowledged gracefully
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "UNKNOWN_EVENT", "user-1", "product-1", "evt-unknown-1",
                null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Renewal_ForUnknownSubscription_ReturnsAccepted()
    {
        // RENEWAL for unknown subscription is acknowledged but does nothing
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "RENEWAL", "unknown-user", "product-1", "evt-renew-1",
                NowUtc, NowUtc.AddDays(30), null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task Handle_Renewal_ResolvesBillingIssue()
    {
        // AC-4: RENEWAL resolves BillingIssue status
        var repository = new InMemorySubscriptionRepository();
        var appUserId = "user-billing-resolve-1";
        var sub = Subscription.CreateForWebhook(
            Guid.NewGuid(), appUserId, "premium_monthly", NowUtc);
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);
        sub.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20));
        await repository.AddAsync(sub, CancellationToken.None);

        var handler = CreateHandler(repository: repository);

        var result = await handler.HandleAsync(
            new ProcessRevenueCatWebhookCommand(
                "sig", "{}", "RENEWAL", appUserId, "premium_monthly", "evt-renew-resolve-1",
                NowUtc.AddDays(30), NowUtc.AddDays(60), null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await repository.GetByRevenueCatAppUserIdAsync(appUserId, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, updated!.Status);
        Assert.Null(updated.BillingIssueDetectedAtUtc);
    }
}
