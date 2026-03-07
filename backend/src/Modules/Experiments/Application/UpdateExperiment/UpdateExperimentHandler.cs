using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.UpdateExperiment;

public sealed class UpdateExperimentHandler(IExperimentRepository repository)
{
    public async Task<UpdateExperimentResult> HandleAsync(
        UpdateExperimentCommand command,
        CancellationToken cancellationToken)
    {
        var experiment = await repository.GetExperimentByIdAsync(command.ExperimentId, cancellationToken);
        if (experiment is null)
        {
            return UpdateExperimentResult.Error(
                ExperimentErrors.ExperimentNotFound,
                "Experiment not found.");
        }

        try
        {
            experiment.UpdateStatus(command.Status);
        }
        catch (ExperimentDomainException ex)
        {
            return UpdateExperimentResult.Error(ex.Code, ex.Message);
        }

        await repository.UpdateExperimentAsync(experiment, cancellationToken);

        return UpdateExperimentResult.Success();
    }
}
