namespace MoneyTracker.Modules.Experiments.Domain;

public sealed class ExperimentAllocation
{
    public ExperimentId ExperimentId { get; }
    public Guid UserId { get; }
    public string VariantName { get; }
    public DateTimeOffset AllocatedAtUtc { get; }

    private ExperimentAllocation(
        ExperimentId experimentId,
        Guid userId,
        string variantName,
        DateTimeOffset allocatedAtUtc)
    {
        ExperimentId = experimentId;
        UserId = userId;
        VariantName = variantName;
        AllocatedAtUtc = allocatedAtUtc;
    }

    public static ExperimentAllocation Create(
        ExperimentId experimentId,
        Guid userId,
        string variantName,
        DateTimeOffset allocatedAtUtc)
    {
        return new ExperimentAllocation(experimentId, userId, variantName, allocatedAtUtc);
    }
}
