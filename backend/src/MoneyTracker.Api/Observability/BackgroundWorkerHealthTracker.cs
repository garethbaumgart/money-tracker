using System.Collections.Concurrent;

namespace MoneyTracker.Api.Observability;

public sealed class BackgroundWorkerHealthTracker
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _heartbeats = new();
    private readonly TimeProvider _timeProvider;

    public BackgroundWorkerHealthTracker(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void RecordHeartbeat(string workerName)
    {
        _heartbeats[workerName] = _timeProvider.GetUtcNow();
    }

    public bool IsHealthy(string workerName)
    {
        if (!_heartbeats.TryGetValue(workerName, out var lastHeartbeat))
        {
            return false;
        }

        var elapsed = _timeProvider.GetUtcNow() - lastHeartbeat;
        return elapsed <= StaleThreshold;
    }

    public IReadOnlyDictionary<string, DateTimeOffset> GetAllHeartbeats()
    {
        return new Dictionary<string, DateTimeOffset>(_heartbeats);
    }
}
