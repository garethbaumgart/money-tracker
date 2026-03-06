namespace MoneyTracker.Modules.Households.Domain;

public sealed class Household
{
    public const int MaxNameLength = 100;

    public HouseholdId Id { get; }
    public string Name { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private Household(HouseholdId id, string name, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public static Household Create(string name, DateTimeOffset nowUtc)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
        {
            throw new HouseholdDomainException(
                HouseholdErrors.ValidationError,
                "Household name is required.");
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new HouseholdDomainException(
                HouseholdErrors.ValidationError,
                $"Household name must be {MaxNameLength} characters or fewer.");
        }

        return new Household(HouseholdId.New(), normalizedName, nowUtc);
    }

    public static string NormalizeName(string? name)
    {
        return name?.Trim() ?? string.Empty;
    }
}
