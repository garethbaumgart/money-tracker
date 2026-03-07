using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.CreateExperiment;

public sealed record CreateExperimentCommand(
    string Name,
    string Description,
    IReadOnlyList<ExperimentVariant> Variants,
    string TargetMetric,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate);
