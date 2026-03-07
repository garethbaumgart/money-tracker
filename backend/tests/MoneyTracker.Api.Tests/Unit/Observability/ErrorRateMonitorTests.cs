using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyTracker.Api.Configuration;
using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Tests.Unit.Observability;

public sealed class ErrorRateMonitorTests
{
    private static AlertingOptions DefaultAlertingOptions => new()
    {
        ErrorRateThresholdPercent = 5,
        ErrorRateWindowSeconds = 300,
    };

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeErrorRate_ReturnsCorrectRate()
    {
        var logger = new RecordingLogger<ErrorRateMonitor>();
        var options = Options.Create(DefaultAlertingOptions);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var monitor = new ErrorRateMonitor(logger, options, timeProvider);

        // Record 10 requests: 1 error (500) and 9 successes (200)
        monitor.RecordRequest("/health", 500);
        for (var i = 0; i < 9; i++)
        {
            monitor.RecordRequest("/health", 200);
        }

        var errorRate = monitor.ComputeErrorRate("/health");

        Assert.Equal(10, errorRate, precision: 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RecordRequest_TriggersAlert_WhenThresholdExceeded()
    {
        var logger = new RecordingLogger<ErrorRateMonitor>();
        var alertingOptions = new AlertingOptions
        {
            ErrorRateThresholdPercent = 5,
            ErrorRateWindowSeconds = 300,
        };
        var options = Options.Create(alertingOptions);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var monitor = new ErrorRateMonitor(logger, options, timeProvider);

        // Record enough errors to trigger alert (100% error rate)
        monitor.RecordRequest("/api/test", 500);

        Assert.Contains(logger.LogEntries, entry =>
            entry.LogLevel == LogLevel.Warning
            && entry.Message.Contains("Error rate alert"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeErrorRate_SlidingWindowExpiresOldEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var logger = new RecordingLogger<ErrorRateMonitor>();
        var alertingOptions = new AlertingOptions
        {
            ErrorRateThresholdPercent = 50,
            ErrorRateWindowSeconds = 60,
        };
        var options = Options.Create(alertingOptions);

        var monitor = new ErrorRateMonitor(logger, options, timeProvider);

        // Record an error at current time
        monitor.RecordRequest("/api/test", 500);

        // Advance time past the window
        timeProvider.Advance(TimeSpan.FromSeconds(61));

        // Record a success at the new time
        monitor.RecordRequest("/api/test", 200);

        // The old error should have been purged, only the success should remain
        var errorRate = monitor.ComputeErrorRate("/api/test");

        Assert.Equal(0, errorRate, precision: 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeErrorRate_ReturnsZero_ForUnknownEndpoint()
    {
        var logger = new RecordingLogger<ErrorRateMonitor>();
        var options = Options.Create(DefaultAlertingOptions);
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var monitor = new ErrorRateMonitor(logger, options, timeProvider);

        var errorRate = monitor.ComputeErrorRate("/unknown");

        Assert.Equal(0, errorRate);
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
}
