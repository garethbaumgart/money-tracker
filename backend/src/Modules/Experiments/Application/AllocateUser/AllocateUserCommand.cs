using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.AllocateUser;

public sealed record AllocateUserCommand(
    ExperimentId ExperimentId,
    Guid UserId);
