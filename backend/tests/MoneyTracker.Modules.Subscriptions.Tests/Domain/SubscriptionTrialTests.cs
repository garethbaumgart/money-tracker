using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Tests.Domain;

public sealed class SubscriptionTrialTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private const string AppUserId = "app-user-trial-1";
    private const string ProductId = "trial_premium";

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_FromNone_SetsTrialStatusAndDates()
    {
        // P4-3-UNIT-01: Subscription.StartTrial with valid household
        var subscription = Subscription.CreateForWebhook(
            HouseholdId, AppUserId, ProductId, NowUtc);

        subscription.StartTrial(NowUtc, trialDays: 14);

        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
        Assert.Equal(NowUtc, subscription.TrialStartedAtUtc);
        Assert.Equal(NowUtc.AddDays(14), subscription.TrialExpiresAtUtc);
        Assert.Equal(NowUtc, subscription.CurrentPeriodStartUtc);
        Assert.Equal(NowUtc.AddDays(14), subscription.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_WhenAlreadyTrial_ThrowsDomainException()
    {
        // P4-3-UNIT-02: StartTrial on subscription already in Trial throws
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.StartTrial(NowUtc.AddDays(1)));

        Assert.Equal(SubscriptionErrors.TrialAlreadyStarted, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_WhenAlreadyActive_ThrowsDomainException()
    {
        // P4-3-UNIT-03: StartTrial on subscription already Active throws
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);
        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.StartTrial(NowUtc.AddDays(1)));

        Assert.Equal(SubscriptionErrors.SubscriptionAlreadyActive, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ExpireTrial_FromTrial_TransitionsToNone()
    {
        // P4-3-UNIT-04: ExpireTrial on Trial subscription past expiry transitions to None
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        subscription.ExpireTrial(NowUtc.AddDays(15));

        Assert.Equal(SubscriptionStatus.None, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ExpireTrial_FromActive_IsNoOp()
    {
        // P4-3-UNIT-05: ExpireTrial on Active subscription has no effect
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);
        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));

        subscription.ExpireTrial(NowUtc.AddDays(15));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ExpireTrial_FromCancelled_IsNoOp()
    {
        // AC-4: Trial expiry does not affect Cancelled status
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);
        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        subscription.Cancel(NowUtc.AddDays(15), "evt-cancel-1", NowUtc.AddDays(15));

        subscription.ExpireTrial(NowUtc.AddDays(20));

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ExpireTrial_FromBillingIssue_IsNoOp()
    {
        // AC-4: Trial expiry does not affect BillingIssue status
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);
        subscription.Activate(NowUtc, NowUtc.AddDays(30), NowUtc, "evt-1", NowUtc.AddMinutes(1));
        subscription.MarkBillingIssue(NowUtc.AddDays(20), "evt-billing-1", NowUtc.AddDays(20));

        subscription.ExpireTrial(NowUtc.AddDays(25));

        Assert.Equal(SubscriptionStatus.BillingIssue, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Trial_ToActive_TransitionSucceeds()
    {
        // Trial -> Active transition (on purchase during trial)
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        subscription.Activate(
            NowUtc.AddDays(5),
            NowUtc.AddDays(35),
            NowUtc.AddDays(5),
            "evt-purchase-1",
            NowUtc.AddDays(5));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(NowUtc.AddDays(35), subscription.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RestoreFromProvider_WithActiveState_UpdatesSubscription()
    {
        // P4-3-UNIT-09: RestoreFromProvider with Active state
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        subscription.RestoreFromProvider(
            SubscriptionStatus.Active,
            "premium_monthly",
            NowUtc,
            NowUtc.AddDays(30));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("premium_monthly", subscription.ProductId);
        Assert.Equal(NowUtc, subscription.CurrentPeriodStartUtc);
        Assert.Equal(NowUtc.AddDays(30), subscription.CurrentPeriodEndUtc);
        // Trial fields should be cleared when restoring to Active
        Assert.Null(subscription.TrialStartedAtUtc);
        Assert.Null(subscription.TrialExpiresAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RestoreFromProvider_WithNoneState_UpdatesToFree()
    {
        // RestoreFromProvider with no active subscription in provider
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        subscription.RestoreFromProvider(
            SubscriptionStatus.None,
            "trial_premium",
            null,
            null);

        Assert.Equal(SubscriptionStatus.None, subscription.Status);
        Assert.Null(subscription.CurrentPeriodStartUtc);
        Assert.Null(subscription.CurrentPeriodEndUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RestoreFromProvider_WithEmptyProductId_ThrowsValidationError()
    {
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, NowUtc.AddDays(14), NowUtc);

        var exception = Assert.Throws<SubscriptionDomainException>(
            () => subscription.RestoreFromProvider(
                SubscriptionStatus.Active, "", NowUtc, NowUtc.AddDays(30)));

        Assert.Equal(SubscriptionErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateTrial_SetsTrialFields()
    {
        // Verify CreateTrial factory sets TrialStartedAtUtc and TrialExpiresAtUtc
        var trialEnd = NowUtc.AddDays(14);
        var subscription = Subscription.CreateTrial(
            HouseholdId, AppUserId, ProductId,
            NowUtc, trialEnd, NowUtc);

        Assert.Equal(NowUtc, subscription.TrialStartedAtUtc);
        Assert.Equal(trialEnd, subscription.TrialExpiresAtUtc);
        Assert.Equal(SubscriptionStatus.Trial, subscription.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_DefaultTrialDaysIs14()
    {
        var subscription = Subscription.CreateForWebhook(
            HouseholdId, AppUserId, ProductId, NowUtc);

        subscription.StartTrial(NowUtc);

        Assert.Equal(NowUtc.AddDays(14), subscription.TrialExpiresAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void StartTrial_CustomTrialDays()
    {
        var subscription = Subscription.CreateForWebhook(
            HouseholdId, AppUserId, ProductId, NowUtc);

        subscription.StartTrial(NowUtc, trialDays: 7);

        Assert.Equal(NowUtc.AddDays(7), subscription.TrialExpiresAtUtc);
    }
}
