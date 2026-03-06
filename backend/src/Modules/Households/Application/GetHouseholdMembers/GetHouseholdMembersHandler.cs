using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.GetHouseholdMembers;

public sealed class GetHouseholdMembersHandler(IHouseholdRepository repository)
{
    public async Task<GetHouseholdMembersResult> HandleAsync(
        GetHouseholdMembersQuery query,
        CancellationToken cancellationToken)
    {
        var household = await repository.GetByIdAsync(query.HouseholdId, cancellationToken);
        if (household is null)
        {
            return GetHouseholdMembersResult.Failure(HouseholdErrors.HouseholdNotFound, "Household not found.");
        }

        if (!household.IsMember(query.RequestingUserId))
        {
            return GetHouseholdMembersResult.Failure(
                HouseholdErrors.HouseholdAccessDenied,
                "User is not a member of this household.");
        }

        return GetHouseholdMembersResult.Success(household.Members.ToArray());
    }
}
