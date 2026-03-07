using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Tests.Domain;

public sealed class SubscriptionTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private const string AppUserId = "app-user-1";
    private const string ProductId = "premium_monthly";

    private static Subscription CreateTrialSubscription()
    {
        return Subscription.CreateTrial(
            HouseholdId,
            AppUserId,
            ProductId,
            NowUtc,
            NowUtc.AddDays(7),
            NowUtc);
    }

    private static Subscription CreateActiveSubscription()
    {
        var sub = CreateTrialSubscription();
        sub.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        return sub;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromTrial_Succeeds()
    {
        // P4-1-UNIT-01: Valid transition Trial -> Active on INITIAL_PURCHASE
        var subscription = CreateTrialSubscription();

        subscription.Activate(
            NowUtc,
            NowUtc.AddDays(30),
            NowUtc,
            "evt-activate-1",
            NowUtc.AddMinutes(1));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(NowUtc, subscription.CurrentPeriodStartUtc);
        Assert.Equal(NowUtc.AddDays(30), subscription.CurrentPeriodEndUtc);
        Assert.Equal(NowUtc, subscription.OriginalPurchaseDateUtc);
        Assert.Equal("evt-activate-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Cancel_FromActive_Succeeds()
    {
        // P4-1-UNIT-02: Valid transition Active -> Cancelled on CANCELLATION
        var subscription = CreateActiveSubscription();
        var cancelDate = NowUtc.AddDays(15);

        subscription.Cancel(cancelDate, "evt-cancel-1", NowUtc.AddDays(15));

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.Equal(cancelDate, subscription.CancelledAtUtc);
        Assert.Equal("evt-cancel-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Expire_FromActive_Succeeds()
    {
        // P4-1-UNIT-03: Valid transition Active -> Expired on EXPIRATION
        var subscription = CreateActiveSubscription();

        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.Equal("evt-expire-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkBillingIssue_FromActive_Succeeds()
    {
        // P4-1-UNIT-04: Valid transition Active -> BillingIssue
        var subscription = CreateActiveSubscription();
        var detectedAt = NowUtc.AddDays(20);

        subscription.MarkBillingIssue(detectedAt, "evt-billing-1", detectedAt);

        Assert.Equal(SubscriptionStatus.BillingIssue, subscription.Status);
        Assert.Equal(detectedAt, subscription.BillingIssueDetectedAtUtc);
        Assert.Equal("evt-billing-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Renew_FromBillingIssue_ResolvesToActive()
    {
        // P4-1-UNIT-05: Valid transition BillingIssue -> Active on RENEWAL
        var subscription = CreateActiveSubscription();
        subscription.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20));

        subscription.Renew(
            NowUtc.AddDays(30),
            NowUtc.AddDays(60),
            "evt-renew-1",
            NowUtc.AddDays(25));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.BillingIssueDetectedAtUtc);
        Assert.Equal(NowUtc.AddDays(60), subscription.CurrentPeriodEndUtc);
        Assert.Equal("evt-renew-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromExpired_ThrowsDomainException()
    {
        // P4-1-UNIT-06: Invalid transition Expired -> Active throws domain exception
        var subscription = CreateActiveSubscription();
        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.Activate(
                NowUtc.AddDays(31),
                NowUtc.AddDays(61),
                NowUtc,
                "evt-reactivate-1",
                NowUtc.AddDays(31)));

        Assert.Equal(SubscriptionErrors.InvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Cancel_FromNone_ThrowsDomainException()
    {
        // P4-1-UNIT-07: Invalid transition None -> Cancelled throws domain exception
        var subscription = Subscription.CreateForWebhook(
            HouseholdId,
            AppUserId,
            ProductId,
            NowUtc);

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.Cancel(NowUtc, "evt-cancel-1", NowUtc));

        Assert.Equal(SubscriptionErrors.InvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Renew_FromActive_ExtendsPeriod()
    {
        // P4-1-UNIT-08: RENEWAL extends CurrentPeriodEndUtc
        var subscription = CreateActiveSubscription();
        var newPeriodEnd = NowUtc.AddDays(60);

        subscription.Renew(
            NowUtc.AddDays(30),
            newPeriodEnd,
            "evt-renew-1",
            NowUtc.AddDays(30));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(newPeriodEnd, subscription.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Cancel_PreservesCancelledAtUtc()
    {
        // P4-1-UNIT-09: CANCELLATION preserves CancelledAtUtc
        var subscription = CreateActiveSubscription();
        var cancelDate = NowUtc.AddDays(15).AddHours(3);

        subscription.Cancel(cancelDate, "evt-cancel-1", NowUtc.AddDays(15));

        Assert.Equal(cancelDate, subscription.CancelledAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ChangeProduct_UpdatesProductId()
    {
        // P4-1-UNIT-10: PRODUCT_CHANGE updates ProductId
        var subscription = CreateActiveSubscription();

        subscription.ChangeProduct("premium_annual", "evt-change-1", NowUtc.AddDays(5));

        Assert.Equal("premium_annual", subscription.ProductId);
        Assert.Equal("evt-change-1", subscription.LastEventId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_FromNone_Succeeds()
    {
        // Valid transition None -> Active (for direct INITIAL_PURCHASE without trial)
        var subscription = Subscription.CreateForWebhook(
            HouseholdId,
            AppUserId,
            ProductId,
            NowUtc);

        subscription.Activate(
            NowUtc,
            NowUtc.AddDays(30),
            NowUtc,
            "evt-purchase-1",
            NowUtc);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Expire_FromCancelled_Succeeds()
    {
        // Valid transition Cancelled -> Expired
        var subscription = CreateActiveSubscription();
        subscription.Cancel(NowUtc.AddDays(15), "evt-cancel-1", NowUtc.AddDays(15));

        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Expire_FromBillingIssue_Succeeds()
    {
        // Valid transition BillingIssue -> Expired
        var subscription = CreateActiveSubscription();
        subscription.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20));

        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Revoke_FromAnyState_Succeeds()
    {
        // Any -> Revoked (TRANSFER or platform revocation)
        var subscription = CreateActiveSubscription();

        subscription.Revoke("evt-revoke-1", NowUtc.AddDays(10));

        Assert.Equal(SubscriptionStatus.Revoked, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Renew_FromExpired_ThrowsDomainException()
    {
        // Cannot renew from Expired
        var subscription = CreateActiveSubscription();
        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.Renew(
                NowUtc.AddDays(31),
                NowUtc.AddDays(61),
                "evt-renew-1",
                NowUtc.AddDays(31)));

        Assert.Equal(SubscriptionErrors.InvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkBillingIssue_FromCancelled_ThrowsDomainException()
    {
        // Cannot mark billing issue from Cancelled
        var subscription = CreateActiveSubscription();
        subscription.Cancel(NowUtc.AddDays(15), "evt-cancel-1", NowUtc.AddDays(15));

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20)));

        Assert.Equal(SubscriptionErrors.InvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ChangeProduct_FromExpired_ThrowsDomainException()
    {
        // Cannot change product when expired
        var subscription = CreateActiveSubscription();
        subscription.Expire("evt-expire-1", NowUtc.AddDays(30));

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.ChangeProduct("other_product", "evt-change-1", NowUtc.AddDays(31)));

        Assert.Equal(SubscriptionErrors.InvalidStateTransition, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void HasProcessedEvent_ReturnsTrueForDuplicateEventId()
    {
        // Idempotency check
        var subscription = CreateTrialSubscription();
        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc);

        Assert.True(subscription.HasProcessedEvent("evt-1"));
        Assert.False(subscription.HasProcessedEvent("evt-2"));
        Assert.False(subscription.HasProcessedEvent(""));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateTrial_WithEmptyAppUserId_ThrowsDomainException()
    {
        var exception = Assert.Throws<SubscriptionDomainException>(
            () => Subscription.CreateTrial(
                HouseholdId,
                "",
                ProductId,
                NowUtc,
                NowUtc.AddDays(7),
                NowUtc));

        Assert.Equal(SubscriptionErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateTrial_WithEmptyProductId_ThrowsDomainException()
    {
        var exception = Assert.Throws<SubscriptionDomainException>(
            () => Subscription.CreateTrial(
                HouseholdId,
                AppUserId,
                "",
                NowUtc,
                NowUtc.AddDays(7),
                NowUtc));

        Assert.Equal(SubscriptionErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateTrial_SetsStatusToTrial()
    {
        var subscription = CreateTrialSubscription();

        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
        Assert.Equal(NowUtc, subscription.CreatedAtUtc);
        Assert.Equal(HouseholdId, subscription.HouseholdId);
        Assert.Equal(AppUserId, subscription.RevenueCatAppUserId);
        Assert.Equal(ProductId, subscription.ProductId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Activate_ClearsBillingIssueAndCancelledDates()
    {
        // Activation clears any previous billing issue or cancellation data
        var subscription = Subscription.CreateForWebhook(
            HouseholdId,
            AppUserId,
            ProductId,
            NowUtc);

        subscription.Activate(
            NowUtc,
            NowUtc.AddDays(30),
            NowUtc,
            "evt-1",
            NowUtc);

        Assert.Null(subscription.CancelledAtUtc);
        Assert.Null(subscription.BillingIssueDetectedAtUtc);
    }
}
