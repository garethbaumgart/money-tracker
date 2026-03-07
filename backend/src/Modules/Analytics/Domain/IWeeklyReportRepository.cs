namespace MoneyTracker.Modules.Analytics.Domain;

public interface IWeeklyReportRepository
{
    Task AddAsync(WeeklyReport report, CancellationToken cancellationToken);

    Task<IReadOnlyList<WeeklyReport>> GetByTypeAsync(
        string reportType,
        int limit,
        CancellationToken cancellationToken);
}
