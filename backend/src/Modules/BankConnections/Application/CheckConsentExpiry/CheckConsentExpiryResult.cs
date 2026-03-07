namespace MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;

public sealed class CheckConsentExpiryResult
{
    private CheckConsentExpiryResult(
        int expiringSoonCount,
        int expiredCount,
        int notificationsCreated)
    {
        ExpiringSoonCount = expiringSoonCount;
        ExpiredCount = expiredCount;
        NotificationsCreated = notificationsCreated;
    }

    public int ExpiringSoonCount { get; }

    public int ExpiredCount { get; }

    public int NotificationsCreated { get; }

    public static CheckConsentExpiryResult Create(
        int expiringSoonCount,
        int expiredCount,
        int notificationsCreated)
    {
        return new CheckConsentExpiryResult(expiringSoonCount, expiredCount, notificationsCreated);
    }
}
