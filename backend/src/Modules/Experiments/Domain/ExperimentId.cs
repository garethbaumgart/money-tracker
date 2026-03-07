namespace MoneyTracker.Modules.Experiments.Domain;

public readonly record struct ExperimentId(Guid Value)
{
    public static ExperimentId New() => new(Guid.NewGuid());
}
