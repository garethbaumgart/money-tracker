using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.AcceptHouseholdInvitation;

public sealed record AcceptHouseholdInvitationCommand(
    string InvitationToken,
    Guid AcceptingUserId,
    string AcceptingEmail);
