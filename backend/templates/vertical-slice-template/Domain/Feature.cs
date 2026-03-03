namespace MoneyTracker.Modules.Feature.Domain;

public sealed class Feature
{
    public FeatureId Id { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private Feature(FeatureId id, string name, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(FeatureErrors.NameRequired);
        }

        Id = id;
        Name = name.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public static Feature Create(string name, DateTimeOffset nowUtc)
    {
        return new Feature(FeatureId.New(), name, nowUtc);
    }
}
