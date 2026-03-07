namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class BankConnection
{
    public BankConnectionId Id { get; }
    public Guid HouseholdId { get; }
    public Guid CreatedByUserId { get; }
    public string ExternalUserId { get; }
    public string ExternalConnectionId { get; private set; }
    public string? ConsentSessionId { get; private set; }
    public string? InstitutionName { get; private set; }
    public BankConnectionStatus Status { get; private set; }
    public ConsentStatus ConsentStatus { get; private set; }
    public DateTimeOffset? ConsentExpiresAtUtc { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public SyncState SyncState { get; } = new();

    private BankConnection(
        BankConnectionId id,
        Guid householdId,
        Guid createdByUserId,
        string externalUserId,
        string externalConnectionId,
        string? consentSessionId,
        string? institutionName,
        BankConnectionStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        HouseholdId = householdId;
        CreatedByUserId = createdByUserId;
        ExternalUserId = externalUserId;
        ExternalConnectionId = externalConnectionId;
        ConsentSessionId = consentSessionId;
        InstitutionName = institutionName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public static BankConnection CreatePending(
        Guid householdId,
        Guid createdByUserId,
        string externalUserId,
        string consentSessionId,
        DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ValidationError,
                "External user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(consentSessionId))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ValidationError,
                "Consent session ID is required.");
        }

        return new BankConnection(
            BankConnectionId.New(),
            householdId,
            createdByUserId,
            externalUserId,
            externalConnectionId: string.Empty,
            consentSessionId,
            institutionName: null,
            BankConnectionStatus.Pending,
            nowUtc);
    }

    public void Activate(string externalConnectionId, string? institutionName, DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(BankConnectionStatus.Active);

        if (string.IsNullOrWhiteSpace(externalConnectionId))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ValidationError,
                "External connection ID is required for activation.");
        }

        ExternalConnectionId = externalConnectionId;
        InstitutionName = institutionName?.Trim();
        Status = BankConnectionStatus.Active;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(BankConnectionStatus.Failed);

        Status = BankConnectionStatus.Failed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        UpdatedAtUtc = nowUtc;
    }

    public void Revoke(DateTimeOffset nowUtc)
    {
        EnsureTransitionAllowed(BankConnectionStatus.Revoked);

        Status = BankConnectionStatus.Revoked;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateConsentSessionId(string consentSessionId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(consentSessionId))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ValidationError,
                "Consent session ID is required.");
        }

        ConsentSessionId = consentSessionId;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateConsentExpiry(DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        ConsentExpiresAtUtc = expiresAtUtc;
        ConsentStatus = ConsentStatus.Active;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkConsentExpiringSoon(DateTimeOffset nowUtc)
    {
        if (ConsentStatus != ConsentStatus.Active)
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ConsentInvalidStateTransition,
                $"Cannot mark consent as expiring soon from {ConsentStatus}.");
        }

        ConsentStatus = ConsentStatus.ExpiringSoon;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkConsentExpired(DateTimeOffset nowUtc)
    {
        if (ConsentStatus is not (ConsentStatus.Active or ConsentStatus.ExpiringSoon))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ConsentInvalidStateTransition,
                $"Cannot mark consent as expired from {ConsentStatus}.");
        }

        ConsentStatus = ConsentStatus.Expired;
        Status = BankConnectionStatus.Expired;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkConsentRevoked(DateTimeOffset nowUtc)
    {
        ConsentStatus = ConsentStatus.Revoked;
        if (Status == BankConnectionStatus.Active)
        {
            Status = BankConnectionStatus.Revoked;
        }
        UpdatedAtUtc = nowUtc;
    }

    public void ReactivateAfterReConsent(string externalConnectionId, string? institutionName, DateTimeOffset consentExpiresAtUtc, DateTimeOffset nowUtc)
    {
        if (Status is not (BankConnectionStatus.Expired or BankConnectionStatus.Revoked))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ReConsentNotNeeded,
                "Connection is still active and does not require re-consent.");
        }

        if (string.IsNullOrWhiteSpace(externalConnectionId))
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ValidationError,
                "External connection ID is required for re-activation.");
        }

        ExternalConnectionId = externalConnectionId;
        InstitutionName = institutionName?.Trim();
        Status = BankConnectionStatus.Active;
        ConsentStatus = ConsentStatus.Active;
        ConsentExpiresAtUtc = consentExpiresAtUtc;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAtUtc = nowUtc;
    }

    public bool IsConsentValid()
    {
        return ConsentStatus is ConsentStatus.Active or ConsentStatus.ExpiringSoon;
    }

    public void RecordSyncSuccess(DateTimeOffset utcNow)
    {
        SyncState.RecordSuccess(utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void RecordSyncFailure(DateTimeOffset utcNow)
    {
        SyncState.RecordFailure(utcNow);
        UpdatedAtUtc = utcNow;
    }

    private void EnsureTransitionAllowed(BankConnectionStatus target)
    {
        var allowed = (Status, target) switch
        {
            (BankConnectionStatus.Pending, BankConnectionStatus.Active) => true,
            (BankConnectionStatus.Pending, BankConnectionStatus.Failed) => true,
            (BankConnectionStatus.Active, BankConnectionStatus.Revoked) => true,
            (BankConnectionStatus.Active, BankConnectionStatus.Expired) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new BankConnectionDomainException(
                BankConnectionErrors.ConnectionInvalidStateTransition,
                $"Cannot transition from {Status} to {target}.");
        }
    }
}
