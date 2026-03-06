using MoneyTracker.Modules.Auth.Application.VerifyCode;

namespace MoneyTracker.Modules.Auth.Application.RefreshSession;

public sealed class RefreshSessionResult
{
    private RefreshSessionResult(bool isSuccess, AuthTokenSet? tokens, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Tokens = tokens;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public AuthTokenSet? Tokens { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static RefreshSessionResult Success(AuthTokenSet tokens)
    {
        return new RefreshSessionResult(
            isSuccess: true,
            tokens: tokens,
            errorCode: null,
            errorMessage: null);
    }

    public static RefreshSessionResult Failure(string errorCode, string errorMessage)
    {
        return new RefreshSessionResult(
            isSuccess: false,
            tokens: null,
            errorCode: errorCode,
            errorMessage: errorMessage);
    }
}
