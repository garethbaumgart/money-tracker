namespace MoneyTracker.Modules.Experiments.Domain;

public sealed class ConversionEvent
{
    public ExperimentId ExperimentId { get; }
    public Guid UserId { get; }
    public string VariantName { get; }
    public DateTimeOffset ConvertedAtUtc { get; }

    private ConversionEvent(
        ExperimentId experimentId,
        Guid userId,
        string variantName,
        DateTimeOffset convertedAtUtc)
    {
        ExperimentId = experimentId;
        UserId = userId;
        VariantName = variantName;
        ConvertedAtUtc = convertedAtUtc;
    }

    public static ConversionEvent Create(
        ExperimentId experimentId,
        Guid userId,
        string variantName,
        DateTimeOffset convertedAtUtc)
    {
        return new ConversionEvent(experimentId, userId, variantName, convertedAtUtc);
    }
}
