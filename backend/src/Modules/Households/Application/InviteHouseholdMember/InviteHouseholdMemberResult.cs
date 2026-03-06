using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.InviteHouseholdMember;

public sealed class InviteHouseholdMemberResult
{
    private InviteHouseholdMemberResult(
        bool isSuccess,
        string? invitationToken,
        DateTimeOffset? invitationExpiresAtUtc,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        InvitationToken = invitationToken;
        InvitationExpiresAtUtc = invitationExpiresAtUtc;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public string? InvitationToken { get; }
    public DateTimeOffset? InvitationExpiresAtUtc { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static InviteHouseholdMemberResult Success(string invitationToken, DateTimeOffset invitationExpiresAtUtc)
    {
        return new InviteHouseholdMemberResult(
            isSuccess: true,
            invitationToken: invitationToken,
            invitationExpiresAtUtc: invitationExpiresAtUtc,
            errorCode: null,
            errorMessage: null);
    }

    public static InviteHouseholdMemberResult Failure(string errorCode, string errorMessage)
    {
        return new InviteHouseholdMemberResult(
            isSuccess: false,
            invitationToken: null,
            invitationExpiresAtUtc: null,
            errorCode: errorCode,
            errorMessage: errorMessage);
    }
}
