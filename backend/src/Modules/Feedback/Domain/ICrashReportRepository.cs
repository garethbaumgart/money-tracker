namespace MoneyTracker.Modules.Feedback.Domain;

public interface ICrashReportRepository
{
    Task<IReadOnlyCollection<CrashReport>> GetRecentAsync(
        int periodDays,
        CancellationToken cancellationToken);
    Task<int> GetTotalCrashCountAsync(
        int periodDays,
        CancellationToken cancellationToken);
}
