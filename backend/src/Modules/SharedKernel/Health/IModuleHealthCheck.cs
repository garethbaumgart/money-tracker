namespace MoneyTracker.Modules.SharedKernel.Health;

public interface IModuleHealthCheck
{
    string ModuleName { get; }
    Task<ModuleHealthResult> CheckAsync(CancellationToken ct);
}
