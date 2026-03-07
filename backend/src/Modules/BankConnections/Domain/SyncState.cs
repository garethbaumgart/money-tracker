namespace MoneyTracker.Modules.BankConnections.Domain;

public sealed class SyncState
{
    public DateTimeOffset? LastSyncCursorUtc { get; private set; }
    public DateTimeOffset? LastSuccessUtc { get; private set; }
    public DateTimeOffset? LastFailureUtc { get; private set; }
    public int ConsecutiveFailures { get; private set; }

    public SyncState()
    {
    }

    public void RecordSuccess(DateTimeOffset utcNow)
    {
        LastSuccessUtc = utcNow;
        LastSyncCursorUtc = utcNow;
        ConsecutiveFailures = 0;
    }

    public void RecordFailure(DateTimeOffset utcNow)
    {
        LastFailureUtc = utcNow;
        ConsecutiveFailures += 1;
    }
}
