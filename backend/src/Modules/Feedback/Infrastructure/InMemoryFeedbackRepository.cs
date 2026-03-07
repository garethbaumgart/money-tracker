using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class InMemoryFeedbackRepository : IFeedbackRepository
{
    private readonly object _sync = new();
    private readonly List<FeedbackItem> _items = [];

    public Task AddAsync(FeedbackItem feedback, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _items.Add(feedback);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(FeedbackItem feedback, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        // In-memory: object is already updated by reference.
        return Task.CompletedTask;
    }

    public Task<FeedbackItem?> GetByIdAsync(FeedbackId id, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<FeedbackItem?>(cancellationToken);
        }

        lock (_sync)
        {
            var item = _items.FirstOrDefault(f => f.Id == id);
            return Task.FromResult(item);
        }
    }

    public Task<IReadOnlyCollection<FeedbackItem>> GetByUserSinceAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<FeedbackItem>>(cancellationToken);
        }

        lock (_sync)
        {
            var items = _items
                .Where(f => f.UserId == userId && f.CreatedAtUtc >= since)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<FeedbackItem>>(items);
        }
    }

    public Task<IReadOnlyCollection<FeedbackItem>> GetByPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<FeedbackItem>>(cancellationToken);
        }

        lock (_sync)
        {
            var items = _items
                .Where(f => f.CreatedAtUtc >= periodStart && f.CreatedAtUtc <= periodEnd)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<FeedbackItem>>(items);
        }
    }

    public Task<int> CountSimilarInPeriodAsync(
        FeedbackCategory category,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        lock (_sync)
        {
            var count = _items.Count(f => f.Category == category && f.CreatedAtUtc >= since);
            return Task.FromResult(count);
        }
    }
}
