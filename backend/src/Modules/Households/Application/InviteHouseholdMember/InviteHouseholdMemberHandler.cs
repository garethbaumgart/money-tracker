using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.InviteHouseholdMember;

public sealed class InviteHouseholdMemberHandler(IHouseholdRepository repository)
{
    public async Task<InviteHouseholdMemberResult> HandleAsync(
        InviteHouseholdMemberCommand command,
        CancellationToken cancellationToken)
    {
        var inviteeEmail = HouseholdInvitation.NormalizeEmail(command.InviteeEmail);
        if (string.IsNullOrWhiteSpace(inviteeEmail))
        {
            return InviteHouseholdMemberResult.Failure(HouseholdErrors.ValidationError, "Invitee email is required.");
        }

        var household = await repository.GetByIdAsync(command.HouseholdId, cancellationToken);
        if (household is null)
        {
            return InviteHouseholdMemberResult.Failure(HouseholdErrors.HouseholdNotFound, "Household not found.");
        }

        if (!household.IsOwner(command.InviterUserId))
        {
            return InviteHouseholdMemberResult.Failure(HouseholdErrors.HouseholdAccessDenied, "Only household owners can invite members.");
        }

        var invitationToken = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTimeOffset.UtcNow.AddDays(7);
        var invitation = new HouseholdInvitation(
            invitationToken,
            household.Id,
            command.InviterUserId,
            inviteeEmail,
            expiresAtUtc);

        var created = await repository.AddInvitationAsync(invitation, cancellationToken);
        if (!created)
        {
            return InviteHouseholdMemberResult.Failure(HouseholdErrors.ValidationError, "Invitation could not be created.");
        }

        return InviteHouseholdMemberResult.Success(invitationToken, expiresAtUtc);
    }
