using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

// PostgreSQL index recommendations:
// - IX_activation_events_user_milestone: UNIQUE index on activation_events(user_id, milestone) for deduplication
// - IX_activation_events_period: index on activation_events(occurred_at_utc) for time-range queries
// - IX_activation_events_platform_region: index on activation_events(LOWER(platform), LOWER(region), occurred_at_utc) for filtered period queries
public sealed class InMemoryActivationEventRepository : IActivationEventRepository
{
    private readonly object _sync = new();
    private readonly List<ActivationEvent> _events = [];
    private readonly HashSet<(Guid UserId, ActivationMilestone Milestone)> _uniqueKeys = [];

    public Task AddAsync(ActivationEvent activationEvent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            var key = (activationEvent.UserId, activationEvent.Milestone);
            if (_uniqueKeys.Add(key))
            {
                _events.Add(activationEvent);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid userId, ActivationMilestone milestone, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult(_uniqueKeys.Contains((userId, milestone)));
        }
    }

    public Task<IReadOnlyCollection<ActivationEvent>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<ActivationEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            return Task.FromResult<IReadOnlyCollection<ActivationEvent>>(_events.ToArray());
        }
    }

    public Task<IReadOnlyCollection<ActivationEvent>> GetByPeriodAsync(
        DateTimeOffset sinceUtc,
        string? platform,
        string? region,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<ActivationEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var query = _events.Where(e => e.OccurredAtUtc >= sinceUtc);

            if (platform is not null)
            {
                query = query.Where(e =>
                    string.Equals(e.Platform, platform, StringComparison.OrdinalIgnoreCase));
            }

            if (region is not null)
            {
                query = query.Where(e =>
                    string.Equals(e.Region, region, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyCollection<ActivationEvent>>(query.ToArray());
        }
    }
}
