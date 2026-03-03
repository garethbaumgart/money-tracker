namespace MoneyTracker.Modules.Feature.Domain;

public readonly record struct FeatureId(Guid Value)
{
    public static FeatureId New() => new(Guid.NewGuid());
}
