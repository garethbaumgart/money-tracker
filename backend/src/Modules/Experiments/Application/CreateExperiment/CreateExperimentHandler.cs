using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.CreateExperiment;

public sealed class CreateExperimentHandler(IExperimentRepository repository)
{
    public async Task<CreateExperimentResult> HandleAsync(
        CreateExperimentCommand command,
        CancellationToken cancellationToken)
    {
        Experiment experiment;
        try
        {
            experiment = Experiment.Create(
                command.Name,
                command.Description,
                command.Variants,
                command.TargetMetric,
                command.StartDate,
                command.EndDate);
        }
        catch (ExperimentDomainException ex)
        {
            return CreateExperimentResult.Error(ex.Code, ex.Message);
        }

        await repository.AddExperimentAsync(experiment, cancellationToken);

        return CreateExperimentResult.Success(experiment.Id.Value);
    }
}
