namespace MoneyTracker.Modules.Feedback.Domain;

public interface INpsRepository
{
    Task AddAsync(NpsScore npsScore, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NpsScore>> GetByPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}
