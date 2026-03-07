using Microsoft.Extensions.Logging;
using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Infrastructure;

/// <summary>
/// HTTP client for RevenueCat REST API v1 subscriber endpoint.
/// Currently delegates to InMemoryRevenueCatClient; will be replaced
/// with real HTTP calls when RevenueCat API key is configured.
/// </summary>
public sealed class RevenueCatClient(
    InMemoryRevenueCatClient innerClient,
    ILogger<RevenueCatClient> logger) : IRevenueCatClient
{
    public async Task<SubscriberInfo?> GetSubscriberAsync(
        string appUserId,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Fetching subscriber info for {AppUserId} from RevenueCat.",
            appUserId);

        // AC-13: In production this would call RevenueCat REST API v1
        // GET /v1/subscribers/{app_user_id}
        // with retry, timeout, and correlation ID logging.
        // For now, delegate to in-memory implementation.
        var result = await innerClient.GetSubscriberAsync(appUserId, cancellationToken);

        if (result is not null)
        {
            logger.LogDebug(
                "Subscriber {AppUserId} found: status={Status}, product={ProductId}.",
                appUserId,
                result.Status,
                result.ProductId);
        }
        else
        {
            logger.LogDebug(
                "No subscriber info found for {AppUserId}.",
                appUserId);
        }

        return result;
    }
}
