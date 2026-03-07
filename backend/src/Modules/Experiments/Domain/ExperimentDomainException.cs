namespace MoneyTracker.Modules.Experiments.Domain;

public sealed class ExperimentDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
