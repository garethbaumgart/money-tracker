using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.RecordConversion;

public sealed record RecordConversionCommand(
    ExperimentId ExperimentId,
    Guid UserId);
