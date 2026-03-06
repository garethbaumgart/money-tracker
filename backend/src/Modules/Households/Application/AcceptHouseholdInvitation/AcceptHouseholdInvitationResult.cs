namespace MoneyTracker.Modules.Households.Application.AcceptHouseholdInvitation;

public sealed class AcceptHouseholdInvitationResult
{
    private AcceptHouseholdInvitationResult(
        bool isSuccess,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static AcceptHouseholdInvitationResult Success()
    {
        return new AcceptHouseholdInvitationResult(isSuccess: true, errorCode: null, errorMessage: null);
    }

    public static AcceptHouseholdInvitationResult Failure(string errorCode, string errorMessage)
    {
        return new AcceptHouseholdInvitationResult(isSuccess: false, errorCode: errorCode, errorMessage: errorMessage);
    }
}
