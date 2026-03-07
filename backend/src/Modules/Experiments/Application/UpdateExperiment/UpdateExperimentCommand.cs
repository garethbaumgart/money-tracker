using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.UpdateExperiment;

public sealed record UpdateExperimentCommand(
    ExperimentId ExperimentId,
    ExperimentStatus Status);
