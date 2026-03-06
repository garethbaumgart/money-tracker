using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.VerifyCode;

public sealed class VerifyCodeResult
{
    private VerifyCodeResult(
        bool isSuccess,
        AuthTokenSet? tokens,
        string? errorCode,
        string? errorMessage)
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

    public static VerifyCodeResult Success(AuthTokenSet tokens)
    {
        return new VerifyCodeResult(
            isSuccess: true,
            tokens: tokens,
            errorCode: null,
            errorMessage: null);
    }

    public static VerifyCodeResult Failure(string errorCode, string errorMessage)
    {
        return new VerifyCodeResult(
            isSuccess: false,
            tokens: null,
            errorCode: errorCode,
            errorMessage: errorMessage);
    }
}
