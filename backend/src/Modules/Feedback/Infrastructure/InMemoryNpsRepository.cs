using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class InMemoryNpsRepository : INpsRepository
{
    private readonly object _sync = new();
    private readonly List<NpsScore> _scores = [];

    public Task AddAsync(NpsScore npsScore, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _scores.Add(npsScore);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<NpsScore>> GetByPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<NpsScore>>(cancellationToken);
        }

        lock (_sync)
        {
            var scores = _scores
                .Where(s => s.RecordedAtUtc >= periodStart && s.RecordedAtUtc <= periodEnd)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<NpsScore>>(scores);
        }
    }
}
