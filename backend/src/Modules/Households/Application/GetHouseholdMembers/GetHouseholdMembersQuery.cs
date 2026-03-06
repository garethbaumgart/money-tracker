using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.GetHouseholdMembers;

public sealed record GetHouseholdMembersQuery(HouseholdId HouseholdId, Guid RequestingUserId);
