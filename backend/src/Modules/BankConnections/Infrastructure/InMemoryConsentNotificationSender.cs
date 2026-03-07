using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class InMemoryConsentNotificationSender : IConsentNotificationSender
{
    private readonly object _sync = new();
    private readonly List<ConsentNotificationRecord> _notifications = [];

    public Task SendConsentExpiringAsync(
        BankConnection connection,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _notifications.Add(new ConsentNotificationRecord(
                connection.Id,
                connection.HouseholdId,
                "consent_expiring",
                connection.InstitutionName));
        }

        return Task.CompletedTask;
    }

    public Task SendConsentExpiredAsync(
        BankConnection connection,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _notifications.Add(new ConsentNotificationRecord(
                connection.Id,
                connection.HouseholdId,
                "consent_expired",
                connection.InstitutionName));
        }

        return Task.CompletedTask;
    }

    public Task SendConsentRevokedAsync(
        BankConnection connection,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _notifications.Add(new ConsentNotificationRecord(
                connection.Id,
                connection.HouseholdId,
                "consent_revoked",
                connection.InstitutionName));
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<ConsentNotificationRecord> GetAll()
    {
        lock (_sync)
        {
            return _notifications.ToArray();
        }
    }
}

public sealed record ConsentNotificationRecord(
    BankConnectionId ConnectionId,
    Guid HouseholdId,
    string NotificationType,
    string? InstitutionName);
