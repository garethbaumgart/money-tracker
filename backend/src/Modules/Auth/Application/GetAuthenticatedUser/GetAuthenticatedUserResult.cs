namespace MoneyTracker.Modules.Auth.Application.GetAuthenticatedUser;

public sealed class GetAuthenticatedUserResult
{
    private GetAuthenticatedUserResult(bool isSuccess, Guid userId, string email, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        UserId = userId;
        Email = email;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public Guid UserId { get; }
    public string Email { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static GetAuthenticatedUserResult Success(Guid userId, string email)
    {
        return new GetAuthenticatedUserResult(
            isSuccess: true,
            userId: userId,
            email: email,
            errorCode: null,
            errorMessage: null);
    }

    public static GetAuthenticatedUserResult Failure(string errorCode, string errorMessage)
    {
        return new GetAuthenticatedUserResult(
            isSuccess: false,
            userId: Guid.Empty,
            email: string.Empty,
            errorCode: errorCode,
            errorMessage: errorMessage);
        }
}
