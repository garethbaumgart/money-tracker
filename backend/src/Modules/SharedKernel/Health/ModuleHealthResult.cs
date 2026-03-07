namespace MoneyTracker.Modules.SharedKernel.Health;

public sealed record ModuleHealthResult(
    ModuleHealthStatus Status,
    long LatencyMs,
    Dictionary<string, object>? Details = null);
