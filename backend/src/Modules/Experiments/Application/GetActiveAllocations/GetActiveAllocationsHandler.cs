using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.GetActiveAllocations;

public sealed class GetActiveAllocationsHandler(IExperimentRepository repository)
{
    public async Task<GetActiveAllocationsResult> HandleAsync(
        GetActiveAllocationsQuery query,
        CancellationToken cancellationToken)
    {
        var activeExperiments = await repository.GetActiveExperimentsAsync(cancellationToken);
        var userAllocations = await repository.GetAllocationsByUserAsync(query.UserId, cancellationToken);

        var activeExperimentIds = activeExperiments.ToDictionary(e => e.Id, e => e);
        var allocations = new List<AllocationDto>();

        foreach (var allocation in userAllocations)
        {
            if (activeExperimentIds.TryGetValue(allocation.ExperimentId, out var experiment))
            {
                allocations.Add(new AllocationDto(
                    experiment.Id.Value,
                    experiment.Name,
                    allocation.VariantName,
                    allocation.AllocatedAtUtc));
            }
        }

        return GetActiveAllocationsResult.Success(allocations);
    }
}
