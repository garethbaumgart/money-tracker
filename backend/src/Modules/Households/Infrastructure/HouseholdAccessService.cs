using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.Households.Infrastructure;

public sealed class HouseholdAccessService(IHouseholdRepository repository) : IHouseholdAccessService
{
    public async Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var household = await repository.GetByIdAsync(new HouseholdId(householdId), cancellationToken);
        if (household is null)
        {
            return HouseholdAccessResult.NotFound();
        }

        return household.IsMember(userId)
            ? HouseholdAccessResult.Allowed()
            : HouseholdAccessResult.Denied();
    }

    public async Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var household = await repository.GetByIdAsync(new HouseholdId(householdId), cancellationToken);
        if (household is null)
        {
            return Array.Empty<Guid>();
        }

        return household.Members.Select(member => member.UserId).ToArray();
    }
}
