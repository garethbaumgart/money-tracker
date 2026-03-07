using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.SharedKernel.Analytics;

namespace MoneyTracker.Modules.Households.Application.AcceptHouseholdInvitation;

public sealed class AcceptHouseholdInvitationHandler(
    IHouseholdRepository repository,
    TimeProvider timeProvider,
    IAnalyticsEventPublisher analyticsPublisher)
{
    public async Task<AcceptHouseholdInvitationResult> HandleAsync(
        AcceptHouseholdInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var invitation = await repository.GetInvitationAsync(command.InvitationToken, cancellationToken);
        if (invitation is null)
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdInvitationNotFound,
                "Invitation token not found.");
        }

        var nowUtc = timeProvider.GetUtcNow();
        if (invitation.IsUsed)
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdInvitationUsed,
                "Invitation already used.");
        }

        if (invitation.IsExpired(nowUtc))
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdInvitationExpired,
                "Invitation token expired.");
        }

        if (!string.Equals(
                invitation.InviteeEmail,
                HouseholdInvitation.NormalizeEmail(command.AcceptingEmail),
                StringComparison.OrdinalIgnoreCase))
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdInvitationEmailMismatch,
                "Invitation email does not match the accepting account.");
        }

        var household = await repository.GetByIdAsync(invitation.HouseholdId, cancellationToken);
        if (household is null)
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdNotFound,
                "Household not found.");
        }

        var added = await repository.AddMemberAsync(invitation.HouseholdId, command.AcceptingUserId, HouseholdRole.Member, cancellationToken);
        if (!added)
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdAlreadyMember,
                "User is already a member of this household.");
        }

        var marked = await repository.MarkInvitationUsedAsync(
            command.InvitationToken,
            command.AcceptingUserId,
            nowUtc,
            cancellationToken);
        if (!marked)
        {
            return AcceptHouseholdInvitationResult.Failure(
                HouseholdErrors.HouseholdInvitationUsed,
                "Invitation already used.");
        }

        await analyticsPublisher.PublishAsync(
            command.AcceptingUserId, "partner_joined", invitation.HouseholdId.Value, cancellationToken);

        return AcceptHouseholdInvitationResult.Success();
    }
}
