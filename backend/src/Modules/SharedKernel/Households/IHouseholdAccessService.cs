namespace MoneyTracker.Modules.SharedKernel.Households;

public readonly record struct HouseholdAccessResult(bool HouseholdExists, bool IsMember)
{
    public static HouseholdAccessResult NotFound() => new(false, false);

    public static HouseholdAccessResult Denied() => new(true, false);

    public static HouseholdAccessResult Allowed() => new(true, true);
}

public interface IHouseholdAccessService
{
    Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(
        Guid householdId,
        CancellationToken cancellationToken);
}
