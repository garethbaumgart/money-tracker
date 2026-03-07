using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class InMemoryCrashReportRepository : ICrashReportRepository
{
    private readonly object _sync = new();
    private readonly List<CrashReport> _reports = [];

    public void Add(CrashReport report)
    {
        lock (_sync)
        {
            _reports.Add(report);
        }
    }

    public Task<IReadOnlyCollection<CrashReport>> GetRecentAsync(
        int periodDays,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<CrashReport>>(cancellationToken);
        }

        lock (_sync)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-periodDays);
            var reports = _reports
                .Where(r => r.LastSeen >= cutoff)
                .OrderByDescending(r => r.Count)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<CrashReport>>(reports);
        }
    }

    public Task<int> GetTotalCrashCountAsync(
        int periodDays,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        lock (_sync)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-periodDays);
            var totalCount = _reports
                .Where(r => r.LastSeen >= cutoff)
                .Sum(r => r.Count);
            return Task.FromResult(totalCount);
        }
    }
}
