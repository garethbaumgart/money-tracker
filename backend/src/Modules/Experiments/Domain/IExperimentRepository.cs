namespace MoneyTracker.Modules.Experiments.Domain;

public interface IExperimentRepository
{
    Task AddExperimentAsync(Experiment experiment, CancellationToken cancellationToken);
    Task UpdateExperimentAsync(Experiment experiment, CancellationToken cancellationToken);
    Task<Experiment?> GetExperimentByIdAsync(ExperimentId id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Experiment>> GetActiveExperimentsAsync(CancellationToken cancellationToken);

    Task AddAllocationAsync(ExperimentAllocation allocation, CancellationToken cancellationToken);
    Task<ExperimentAllocation?> GetAllocationAsync(ExperimentId experimentId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExperimentAllocation>> GetAllocationsByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ExperimentAllocation>> GetAllocationsByExperimentAsync(ExperimentId experimentId, CancellationToken cancellationToken);

    Task AddConversionEventAsync(ConversionEvent conversionEvent, CancellationToken cancellationToken);
    Task<ConversionEvent?> GetConversionEventAsync(ExperimentId experimentId, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ConversionEvent>> GetConversionEventsByExperimentAsync(ExperimentId experimentId, CancellationToken cancellationToken);
}
