using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class InMemorySyncEventRepository : ISyncEventRepository
{
    private readonly object _sync = new();
    private readonly List<SyncEvent> _events = [];

    public Task AddAsync(SyncEvent syncEvent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _events.Add(syncEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<SyncEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var result = _events
                .Where(e => e.OccurredAtUtc >= since)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(result);
        }
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByRegionAsync(string region, DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<SyncEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var result = _events
                .Where(e => e.OccurredAtUtc >= since
                    && e.Region.Equals(region, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(result);
        }
    }

    public Task<IReadOnlyCollection<SyncEvent>> GetByInstitutionAsync(string institution, DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<SyncEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var result = _events
                .Where(e => e.OccurredAtUtc >= since
                    && e.Institution.Equals(institution, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<SyncEvent>>(result);
        }
    }
}
