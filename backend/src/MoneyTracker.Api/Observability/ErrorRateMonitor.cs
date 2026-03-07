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
        RecordRequest(endpoint, statusCode, durationMs: null);
    }

    public void RecordRequest(string endpoint, int statusCode, long? durationMs)
    {
        var now = timeProvider.GetUtcNow();
        var record = new RequestRecord(statusCode, now, durationMs);

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

    /// <summary>
    /// Returns the overall 5xx error rate across all endpoints within the sliding window.
    /// </summary>
    public double ComputeOverallErrorRate()
    {
        var now = timeProvider.GetUtcNow();
        var windowStart = now.AddSeconds(-alertingOptions.Value.ErrorRateWindowSeconds);

        var allRecords = _records.Values
            .SelectMany(bag => bag.Where(r => r.TimestampUtc >= windowStart))
            .ToArray();

        if (allRecords.Length == 0)
        {
            return 0;
        }

        var errorCount = allRecords.Count(r => r.StatusCode >= 500);
        return (double)errorCount / allRecords.Length * 100;
    }

    /// <summary>
    /// Computes latency percentiles (p50, p95, p99) across all endpoints within the sliding window.
    /// </summary>
    public LatencyPercentiles ComputeLatencyPercentiles()
    {
        var now = timeProvider.GetUtcNow();
        var windowStart = now.AddSeconds(-alertingOptions.Value.ErrorRateWindowSeconds);

        var durations = _records.Values
            .SelectMany(bag => bag.Where(r => r.TimestampUtc >= windowStart && r.DurationMs.HasValue))
            .Select(r => r.DurationMs!.Value)
            .OrderBy(d => d)
            .ToArray();

        if (durations.Length == 0)
        {
            return new LatencyPercentiles(0, 0, 0);
        }

        return new LatencyPercentiles(
            Percentile(durations, 50),
            Percentile(durations, 95),
            Percentile(durations, 99));
    }

    private static long Percentile(long[] sorted, int percentile)
    {
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Length) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Length - 1))];
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

    private sealed record RequestRecord(int StatusCode, DateTimeOffset TimestampUtc, long? DurationMs = null);
}

public sealed record LatencyPercentiles(long P50Ms, long P95Ms, long P99Ms);
