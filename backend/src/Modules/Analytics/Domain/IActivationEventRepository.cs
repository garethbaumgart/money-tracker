namespace MoneyTracker.Modules.Analytics.Domain;

public interface IActivationEventRepository
{
    Task AddAsync(ActivationEvent activationEvent, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid userId, ActivationMilestone milestone, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ActivationEvent>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ActivationEvent>> GetByPeriodAsync(
        DateTimeOffset sinceUtc,
        string? platform,
        string? region,
        CancellationToken cancellationToken);
}
