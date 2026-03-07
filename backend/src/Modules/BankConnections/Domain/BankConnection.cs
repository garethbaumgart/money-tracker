namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class BankConnection
{
    public BankConnectionId Id { get; }
    public Guid HouseholdId { get; }
    public Guid CreatedByUserId { get; }
    public string ExternalUserId { get; }
    public string ExternalConnectionId { get; private set; }
    public string? ConsentSessionId { get; }
    public string? InstitutionName { get; private set; }
    public BankConnectionStatus Status { get; private set; }
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
