namespace MoneyTracker.Modules.Households.Domain;

public readonly record struct HouseholdId(Guid Value)
{
    public static HouseholdId New() => new(Guid.NewGuid());
}
