using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Application.AllocateUser;

public sealed class AllocateUserHandler(IExperimentRepository repository)
{
    public async Task<AllocateUserResult> HandleAsync(
        AllocateUserCommand command,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentByIdAsync(command.ExperimentId, cancellationToken);
        if (experiment is null)
        {
            return AllocateUserResult.Error(
                ExperimentErrors.ExperimentNotFound,
                "Experiment not found.");
        }

        if (experiment.Status != ExperimentStatus.Active)
        {
            return AllocateUserResult.Error(
                ExperimentErrors.ExperimentNotActive,
                "Experiment is not active.");
        }

        // Sticky assignment: check existing allocation first
        var existingAllocation = await repository.GetAllocationAsync(command.ExperimentId, command.UserId, cancellationToken);
        if (existingAllocation is not null)
        {
            return AllocateUserResult.Success(
                experiment.Id,
                experiment.Name,
                existingAllocation.VariantName,
                existingAllocation.AllocatedAtUtc);
        }

        // Deterministic allocation via SHA256 hash
        var variantName = HashBasedVariantAllocator.Allocate(
            command.ExperimentId,
            command.UserId,
            experiment.Variants);

        var allocation = ExperimentAllocation.Create(
            command.ExperimentId,
            command.UserId,
            variantName,
            DateTimeOffset.UtcNow);

        await repository.AddAllocationAsync(allocation, cancellationToken);

        return AllocateUserResult.Success(
            experiment.Id,
            experiment.Name,
            variantName,
            allocation.AllocatedAtUtc);
    }
}
