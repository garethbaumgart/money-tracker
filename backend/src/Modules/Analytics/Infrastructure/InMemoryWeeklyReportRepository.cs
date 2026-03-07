using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Infrastructure;

public sealed class InMemoryWeeklyReportRepository : IWeeklyReportRepository
{
    private readonly object _sync = new();
    private readonly List<WeeklyReport> _reports = [];

    public Task AddAsync(WeeklyReport report, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _reports.Add(report);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WeeklyReport>> GetByTypeAsync(
        string reportType,
        int limit,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyList<WeeklyReport>>(cancellationToken);
        }

        lock (_sync)
        {
            var result = _reports
                .Where(r => string.Equals(r.ReportType, reportType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.GeneratedAtUtc)
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<WeeklyReport>>(result);
        }
    }
}
