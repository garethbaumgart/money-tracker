using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.CheckConsentExpiry;

public sealed class CheckConsentExpiryHandler(
    IBankConnectionRepository connectionRepository,
    IConsentNotificationSender notificationSender,
    TimeProvider timeProvider,
    ILogger<CheckConsentExpiryHandler> logger)
{
    private static readonly TimeSpan ExpiringSoonThreshold = TimeSpan.FromDays(7);

    public async Task<CheckConsentExpiryResult> HandleAsync(
        CheckConsentExpiryCommand command,
        CancellationToken cancellationToken)
    {
        var connections = await connectionRepository.GetAllConnectionsAsync(cancellationToken);
        var nowUtc = timeProvider.GetUtcNow();

        var expiringSoonCount = 0;
        var expiredCount = 0;
        var notificationsCreated = 0;

        foreach (var connection in connections)
        {
            if (connection.ConsentExpiresAtUtc is null)
            {
                continue;
            }

            if (connection.ConsentStatus is ConsentStatus.Revoked)
            {
                continue;
            }

            var timeUntilExpiry = connection.ConsentExpiresAtUtc.Value - nowUtc;

            if (timeUntilExpiry <= TimeSpan.Zero
                && connection.ConsentStatus is ConsentStatus.Active or ConsentStatus.ExpiringSoon)
            {
                // Connection has expired
                try
                {
                    connection.MarkConsentExpired(nowUtc);
                    await connectionRepository.UpdateAsync(connection, cancellationToken);
                    await notificationSender.SendConsentExpiredAsync(connection, cancellationToken);
                    expiredCount++;
                    notificationsCreated++;
                    logger.LogInformation(
                        "Consent expired for connection={ConnectionId} household={HouseholdId}",
                        connection.Id.Value,
                        connection.HouseholdId);
                }
                catch (BankConnectionDomainException ex)
                {
                    logger.LogWarning(
                        ex,
                        "Could not mark consent expired for connection={ConnectionId}: {Message}",
                        connection.Id.Value,
                        ex.Message);
                }
            }
            else if (timeUntilExpiry <= ExpiringSoonThreshold
                     && timeUntilExpiry > TimeSpan.Zero
                     && connection.ConsentStatus == ConsentStatus.Active)
            {
                // Connection is expiring soon
                try
                {
                    connection.MarkConsentExpiringSoon(nowUtc);
                    await connectionRepository.UpdateAsync(connection, cancellationToken);
                    await notificationSender.SendConsentExpiringAsync(connection, cancellationToken);
                    expiringSoonCount++;
                    notificationsCreated++;
                    logger.LogInformation(
                        "Consent expiring soon for connection={ConnectionId} household={HouseholdId} expiresAt={ExpiresAt}",
                        connection.Id.Value,
                        connection.HouseholdId,
                        connection.ConsentExpiresAtUtc);
                }
                catch (BankConnectionDomainException ex)
                {
                    logger.LogWarning(
                        ex,
                        "Could not mark consent as expiring soon for connection={ConnectionId}: {Message}",
                        connection.Id.Value,
                        ex.Message);
                }
            }
            // Connections expiring in 30+ days → no action
        }

        return CheckConsentExpiryResult.Create(expiringSoonCount, expiredCount, notificationsCreated);
    }
}
