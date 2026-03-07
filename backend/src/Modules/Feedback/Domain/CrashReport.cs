namespace MoneyTracker.Modules.Feedback.Domain;

public sealed class CrashReport
{
    public string Signature { get; }
    public int Count { get; private set; }
    public int AffectedUsers { get; private set; }
    public DateTimeOffset FirstSeen { get; }
    public DateTimeOffset LastSeen { get; private set; }

    public CrashReport(
        string signature,
        int count,
        int affectedUsers,
        DateTimeOffset firstSeen,
        DateTimeOffset lastSeen)
    {
        Signature = signature;
        Count = count;
        AffectedUsers = affectedUsers;
        FirstSeen = firstSeen;
        LastSeen = lastSeen;
    }

    public void IncrementCount(DateTimeOffset nowUtc)
    {
        Count++;
        LastSeen = nowUtc;
    }

    public void IncrementAffectedUsers()
    {
        AffectedUsers++;
    }
}
