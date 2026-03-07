using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Tests.Unit.Observability;

public sealed class BackgroundWorkerHealthTrackerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void IsHealthy_ReturnsFalse_WhenNoHeartbeatRecorded()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        var result = tracker.IsHealthy("UnknownWorker");

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsHealthy_ReturnsTrue_WhenRecentHeartbeat()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        tracker.RecordHeartbeat("TestWorker");

        var result = tracker.IsHealthy("TestWorker");

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsHealthy_ReturnsFalse_WhenHeartbeatIsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        tracker.RecordHeartbeat("TestWorker");

        // Advance time past the 5-minute stale threshold
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        var result = tracker.IsHealthy("TestWorker");

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsHealthy_ReturnsTrue_WhenHeartbeatIsExactlyAtThreshold()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        tracker.RecordHeartbeat("TestWorker");

        // Advance time to exactly 5 minutes (should still be healthy)
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        var result = tracker.IsHealthy("TestWorker");

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetAllHeartbeats_ReturnsAllRecordedWorkers()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        tracker.RecordHeartbeat("Worker1");
        tracker.RecordHeartbeat("Worker2");
        tracker.RecordHeartbeat("Worker3");

        var heartbeats = tracker.GetAllHeartbeats();

        Assert.Equal(3, heartbeats.Count);
        Assert.Contains("Worker1", heartbeats.Keys);
        Assert.Contains("Worker2", heartbeats.Keys);
        Assert.Contains("Worker3", heartbeats.Keys);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RecordHeartbeat_UpdatesExistingEntry()
    {
        var now = DateTimeOffset.UtcNow;
        var timeProvider = new FakeTimeProvider(now);
        var tracker = new BackgroundWorkerHealthTracker(timeProvider);

        tracker.RecordHeartbeat("TestWorker");

        timeProvider.Advance(TimeSpan.FromMinutes(3));
        tracker.RecordHeartbeat("TestWorker");

        timeProvider.Advance(TimeSpan.FromMinutes(3));

        // Should still be healthy because second heartbeat was only 3 minutes ago
        var result = tracker.IsHealthy("TestWorker");

        Assert.True(result);
    }
}
