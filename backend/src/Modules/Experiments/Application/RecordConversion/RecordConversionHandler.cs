using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.RecordConversion;

public sealed class RecordConversionHandler(IExperimentRepository repository)
{
    public async Task<RecordConversionResult> HandleAsync(
        RecordConversionCommand command,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentByIdAsync(command.ExperimentId, cancellationToken);
        if (experiment is null)
        {
            return RecordConversionResult.Error(
                ExperimentErrors.ExperimentNotFound,
                "Experiment not found.");
        }

        var allocation = await repository.GetAllocationAsync(command.ExperimentId, command.UserId, cancellationToken);
        if (allocation is null)
        {
            return RecordConversionResult.Error(
                ExperimentErrors.AllocationNotFound,
                "User is not allocated to this experiment.");
        }

        // Idempotent: ignore duplicate conversions
        var existingConversion = await repository.GetConversionEventAsync(command.ExperimentId, command.UserId, cancellationToken);
        if (existingConversion is not null)
        {
            return RecordConversionResult.Success();
        }

        var conversionEvent = ConversionEvent.Create(
            command.ExperimentId,
            command.UserId,
            allocation.VariantName,
            DateTimeOffset.UtcNow);

        await repository.AddConversionEventAsync(conversionEvent, cancellationToken);

        return RecordConversionResult.Success();
    }
}
