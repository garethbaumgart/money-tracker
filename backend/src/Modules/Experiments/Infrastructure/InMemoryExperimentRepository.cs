using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Infrastructure;

public sealed class InMemoryExperimentRepository : IExperimentRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<ExperimentId, Experiment> _experiments = new();
    private readonly List<ExperimentAllocation> _allocations = new();
    private readonly List<ConversionEvent> _conversions = new();

    public Task AddExperimentAsync(Experiment experiment, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _experiments[experiment.Id] = experiment;
        }

        return Task.CompletedTask;
    }

    public Task UpdateExperimentAsync(Experiment experiment, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _experiments[experiment.Id] = experiment;
        }

        return Task.CompletedTask;
    }

    public Task<Experiment?> GetExperimentByIdAsync(ExperimentId id, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Experiment?>(cancellationToken);
        }

        lock (_sync)
        {
            _experiments.TryGetValue(id, out var experiment);
            return Task.FromResult(experiment);
        }
    }

    public Task<IReadOnlyCollection<Experiment>> GetActiveExperimentsAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<Experiment>>(cancellationToken);
        }

        lock (_sync)
        {
            var active = _experiments.Values
                .Where(e => e.Status == ExperimentStatus.Active)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<Experiment>>(active);
        }
    }

    public Task AddAllocationAsync(ExperimentAllocation allocation, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _allocations.Add(allocation);
        }

        return Task.CompletedTask;
    }

    public Task<ExperimentAllocation?> GetAllocationAsync(ExperimentId experimentId, Guid userId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<ExperimentAllocation?>(cancellationToken);
        }

        lock (_sync)
        {
            var allocation = _allocations
                .FirstOrDefault(a => a.ExperimentId == experimentId && a.UserId == userId);
            return Task.FromResult(allocation);
        }
    }

    public Task<IReadOnlyCollection<ExperimentAllocation>> GetAllocationsByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<ExperimentAllocation>>(cancellationToken);
        }

        lock (_sync)
        {
            var allocations = _allocations
                .Where(a => a.UserId == userId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ExperimentAllocation>>(allocations);
        }
    }

    public Task<IReadOnlyCollection<ExperimentAllocation>> GetAllocationsByExperimentAsync(ExperimentId experimentId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<ExperimentAllocation>>(cancellationToken);
        }

        lock (_sync)
        {
            var allocations = _allocations
                .Where(a => a.ExperimentId == experimentId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ExperimentAllocation>>(allocations);
        }
    }

    public Task AddConversionEventAsync(ConversionEvent conversionEvent, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        lock (_sync)
        {
            _conversions.Add(conversionEvent);
        }

        return Task.CompletedTask;
    }

    public Task<ConversionEvent?> GetConversionEventAsync(ExperimentId experimentId, Guid userId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<ConversionEvent?>(cancellationToken);
        }

        lock (_sync)
        {
            var conversion = _conversions
                .FirstOrDefault(c => c.ExperimentId == experimentId && c.UserId == userId);
            return Task.FromResult(conversion);
        }
    }

    public Task<IReadOnlyCollection<ConversionEvent>> GetConversionEventsByExperimentAsync(ExperimentId experimentId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<ConversionEvent>>(cancellationToken);
        }

        lock (_sync)
        {
            var conversions = _conversions
                .Where(c => c.ExperimentId == experimentId)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ConversionEvent>>(conversions);
        }
    }
}
