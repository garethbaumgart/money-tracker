using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;

namespace MoneyTracker.Api.Observability;

internal sealed class ErrorRateMonitor(
    ILogger<ErrorRateMonitor> logger,
    IOptions<AlertingOptions> alertingOptions,
    TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<RequestRecord>> _records = new();

    public void RecordRequest(string endpoint, int statusCode)
    {
        var now = timeProvider.GetUtcNow();
        var record = new RequestRecord(statusCode, now);

        var bag = _records.GetOrAdd(endpoint, _ => []);
        bag.Add(record);

        PurgeExpiredEntries(endpoint, now);

        var errorRate = ComputeErrorRate(endpoint, now);
        if (errorRate >= alertingOptions.Value.ErrorRateThresholdPercent)
        {
            logger.LogWarning(
                "Error rate alert for endpoint {Endpoint}: rate={ErrorRate:F1}% threshold={Threshold}% window={WindowSeconds}s",
                endpoint,
                errorRate,
                alertingOptions.Value.ErrorRateThresholdPercent,
                alertingOptions.Value.ErrorRateWindowSeconds);
        }
    }

    /// <summary>
    /// Computes the current 5xx error rate percentage for the given endpoint
    /// within the configured sliding window.
    /// </summary>
    public double ComputeErrorRate(string endpoint, DateTimeOffset? asOf = null)
    {
        var now = asOf ?? timeProvider.GetUtcNow();
        var windowStart = now.AddSeconds(-alertingOptions.Value.ErrorRateWindowSeconds);

        if (!_records.TryGetValue(endpoint, out var bag))
        {
            return 0;
        }

        var recentRecords = bag.Where(r => r.TimestampUtc >= windowStart).ToArray();
        if (recentRecords.Length == 0)
        {
            return 0;
        }

        var errorCount = recentRecords.Count(r => r.StatusCode >= 500);
        return (double)errorCount / recentRecords.Length * 100;
    }

    private void PurgeExpiredEntries(string endpoint, DateTimeOffset now)
    {
        var windowStart = now.AddSeconds(-alertingOptions.Value.ErrorRateWindowSeconds);

        if (!_records.TryGetValue(endpoint, out var bag))
        {
            return;
        }

        var valid = bag.Where(r => r.TimestampUtc >= windowStart).ToArray();
        if (valid.Length < bag.Count)
        {
            var newBag = new ConcurrentBag<RequestRecord>(valid);
            _records.TryUpdate(endpoint, newBag, bag);
        }
    }

    private sealed record RequestRecord(int StatusCode, DateTimeOffset TimestampUtc);
}
