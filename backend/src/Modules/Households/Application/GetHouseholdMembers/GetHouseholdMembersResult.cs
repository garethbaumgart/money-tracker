using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Application.GetHouseholdMembers;

public sealed class GetHouseholdMembersResult
{
    private GetHouseholdMembersResult(
        bool isSuccess,
        HouseholdMember[]? members,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        Members = members;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public HouseholdMember[]? Members { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static GetHouseholdMembersResult Success(HouseholdMember[] members)
    {
        return new GetHouseholdMembersResult(isSuccess: true, members: members, errorCode: null, errorMessage: null);
    }

    public static GetHouseholdMembersResult Failure(string errorCode, string errorMessage)
    {
        return new GetHouseholdMembersResult(isSuccess: false, members: null, errorCode: errorCode, errorMessage: errorMessage);
    }
}
