using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class InMemoryLinkEventRepository : ILinkEventRepository
{
    private readonly object _sync = new();
    private readonly List<LinkEvent> _events = [];

    public Task AddAsync(LinkEvent linkEvent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _events.Add(linkEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<LinkEvent>> GetByPeriodAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<LinkEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var result = _events
                .Where(e => e.OccurredAtUtc >= since)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<LinkEvent>>(result);
        }
    }
}
