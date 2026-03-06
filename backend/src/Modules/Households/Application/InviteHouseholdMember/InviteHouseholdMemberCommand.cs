using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.InviteHouseholdMember;

public sealed record InviteHouseholdMemberCommand(HouseholdId HouseholdId, Guid InviterUserId, string InviteeEmail);
