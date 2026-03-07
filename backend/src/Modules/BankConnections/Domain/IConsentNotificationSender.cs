namespace MoneyTracker.Modules.BankConnections.Domain;

public interface IConsentNotificationSender
{
    Task SendConsentExpiringAsync(
        BankConnection connection,
        CancellationToken cancellationToken);

    Task SendConsentExpiredAsync(
        BankConnection connection,
        CancellationToken cancellationToken);

    Task SendConsentRevokedAsync(
        BankConnection connection,
        CancellationToken cancellationToken);
}
