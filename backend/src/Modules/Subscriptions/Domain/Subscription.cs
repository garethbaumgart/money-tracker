namespace MoneyTracker.Modules.Subscriptions.Domain;

public sealed class Subscription
{
    public SubscriptionId Id { get; }
    public Guid HouseholdId { get; }
    public string RevenueCatAppUserId { get; }
    public string ProductId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset? CurrentPeriodStartUtc { get; private set; }
    public DateTimeOffset? CurrentPeriodEndUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public DateTimeOffset? BillingIssueDetectedAtUtc { get; private set; }
    public DateTimeOffset? TrialStartedAtUtc { get; private set; }
    public DateTimeOffset? TrialExpiresAtUtc { get; private set; }
    public DateTimeOffset? OriginalPurchaseDateUtc { get; private set; }
    public string? LastEventId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Subscription(
        SubscriptionId id,
        Guid householdId,
        string revenueCatAppUserId,
        string productId,
        SubscriptionStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        RevenueCatAppUserId = revenueCatAppUserId;
        ProductId = productId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a new subscription in Trial status (used by P4-3 for auto-granting trials).
    /// </summary>
    public static Subscription CreateTrial(
        Guid householdId,
        string revenueCatAppUserId,
        string productId,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(revenueCatAppUserId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "RevenueCat app user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "Product ID is required.");
        }

        var subscription = new Subscription(
            SubscriptionId.New(),
            householdId,
            revenueCatAppUserId,
            productId,
            SubscriptionStatus.Trial,
            nowUtc)
        {
            CurrentPeriodStartUtc = periodStartUtc,
            CurrentPeriodEndUtc = periodEndUtc,
            TrialStartedAtUtc = nowUtc,
            TrialExpiresAtUtc = periodEndUtc
        };

        return subscription;
    }

    /// <summary>
    /// Creates a new subscription in None status, to be activated by a webhook event.
    /// </summary>
    public static Subscription CreateForWebhook(
        Guid householdId,
        string revenueCatAppUserId,
        string productId,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(revenueCatAppUserId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "RevenueCat app user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "Product ID is required.");
        }

        return new Subscription(
            SubscriptionId.New(),
            householdId,
            revenueCatAppUserId,
            productId,
            SubscriptionStatus.None,
            nowUtc);
    }

    /// <summary>
    /// Activates the subscription from Trial or None status (INITIAL_PURCHASE).
    /// </summary>
    public void Activate(
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset originalPurchaseDateUtc,
        string eventId,
        DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(SubscriptionStatus.Active);

        Status = SubscriptionStatus.Active;
        CurrentPeriodStartUtc = periodStartUtc;
        CurrentPeriodEndUtc = periodEndUtc;
        OriginalPurchaseDateUtc = originalPurchaseDateUtc;
        CancelledAtUtc = null;
        BillingIssueDetectedAtUtc = null;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Renews the subscription, extending the period (RENEWAL).
    /// Also resolves BillingIssue status.
    /// </summary>
    public void Renew(
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        string eventId,
        DateTimeOffset nowUtc)
    {
        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.BillingIssue))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.InvalidStateTransition,
                $"Cannot renew subscription from {Status}.");
        }

        Status = SubscriptionStatus.Active;
        CurrentPeriodStartUtc = periodStartUtc;
        CurrentPeriodEndUtc = periodEndUtc;
        BillingIssueDetectedAtUtc = null;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Marks the subscription as cancelled (CANCELLATION).
    /// Subscription remains active until period end.
    /// </summary>
    public void Cancel(
        DateTimeOffset cancelledAtUtc,
        string eventId,
        DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(SubscriptionStatus.Cancelled);

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Expires the subscription (EXPIRATION). Access is revoked.
    /// </summary>
    public void Expire(
        string eventId,
        DateTimeOffset nowUtc)
    {
        if (Status is not (SubscriptionStatus.Active or SubscriptionStatus.Cancelled or SubscriptionStatus.BillingIssue))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.InvalidStateTransition,
                $"Cannot expire subscription from {Status}.");
        }

        Status = SubscriptionStatus.Expired;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Marks a billing issue on the subscription (BILLING_ISSUE).
    /// </summary>
    public void MarkBillingIssue(
        DateTimeOffset detectedAtUtc,
        string eventId,
        DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(SubscriptionStatus.BillingIssue);

        Status = SubscriptionStatus.BillingIssue;
        BillingIssueDetectedAtUtc = detectedAtUtc;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Updates the product ID on the subscription (PRODUCT_CHANGE).
    /// </summary>
    public void ChangeProduct(
        string newProductId,
        string eventId,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(newProductId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "New product ID is required.");
        }

        if (Status is SubscriptionStatus.None or SubscriptionStatus.Expired or SubscriptionStatus.Revoked)
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.InvalidStateTransition,
                $"Cannot change product when subscription is {Status}.");
        }

        ProductId = newProductId;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Revokes the subscription (TRANSFER or platform revocation).
    /// </summary>
    public void Revoke(
        string eventId,
        DateTimeOffset nowUtc)
    {
        Status = SubscriptionStatus.Revoked;
        LastEventId = eventId;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Starts a trial period on this subscription.
    /// Can only be called when the subscription is in None status (no existing trial or active subscription).
    /// </summary>
    public void StartTrial(DateTimeOffset nowUtc, int trialDays = 14)
    {
        if (Status is SubscriptionStatus.Trial)
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.TrialAlreadyStarted,
                "A trial has already been started for this subscription.");
        }

        if (Status is SubscriptionStatus.Active)
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.SubscriptionAlreadyActive,
                "Cannot start a trial on an already active subscription.");
        }

        if (Status is not SubscriptionStatus.None)
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.InvalidStateTransition,
                $"Cannot start trial from {Status}.");
        }

        Status = SubscriptionStatus.Trial;
        TrialStartedAtUtc = nowUtc;
        TrialExpiresAtUtc = nowUtc.AddDays(trialDays);
        CurrentPeriodStartUtc = nowUtc;
        CurrentPeriodEndUtc = nowUtc.AddDays(trialDays);
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Expires the trial, transitioning the subscription to None status.
    /// Only applies when the subscription is in Trial status.
    /// </summary>
    public void ExpireTrial(DateTimeOffset nowUtc)
    {
        if (Status is not SubscriptionStatus.Trial)
        {
            return; // No-op for non-trial subscriptions (AC-4)
        }

        Status = SubscriptionStatus.None;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>
    /// Restores the subscription state from the payment provider (RevenueCat).
    /// This is the authoritative state reconciliation method.
    /// </summary>
    public void RestoreFromProvider(
        SubscriptionStatus status,
        string productId,
        DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.ValidationError,
                "Product ID is required for restore.");
        }

        Status = status;
        ProductId = productId;
        CurrentPeriodStartUtc = periodStart;
        CurrentPeriodEndUtc = periodEnd;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Clear trial fields if transitioning away from trial
        if (status is not SubscriptionStatus.Trial)
        {
            TrialStartedAtUtc = null;
            TrialExpiresAtUtc = null;
        }

        // Clear cancellation/billing issue fields when restoring to active
        if (status is SubscriptionStatus.Active)
        {
            CancelledAtUtc = null;
            BillingIssueDetectedAtUtc = null;
        }
    }

    /// <summary>
    /// Checks if the given event has already been processed (idempotency).
    /// </summary>
    public bool HasProcessedEvent(string eventId)
    {
        return !string.IsNullOrWhiteSpace(LastEventId)
               && string.Equals(LastEventId, eventId, StringComparison.Ordinal);
    }

    private void EnsureTransitionAllowed(SubscriptionStatus target)
    {
        var allowed = (Status, target) switch
        {
            (SubscriptionStatus.None, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.None, SubscriptionStatus.Trial) => true,
            (SubscriptionStatus.Trial, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.Trial, SubscriptionStatus.None) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.Cancelled) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.Expired) => true,
            (SubscriptionStatus.Active, SubscriptionStatus.BillingIssue) => true,
            (SubscriptionStatus.Cancelled, SubscriptionStatus.Expired) => true,
            (SubscriptionStatus.BillingIssue, SubscriptionStatus.Active) => true,
            (SubscriptionStatus.BillingIssue, SubscriptionStatus.Expired) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new SubscriptionDomainException(
                SubscriptionErrors.InvalidStateTransition,
                $"Cannot transition from {Status} to {target}.");
        }
    }
}
