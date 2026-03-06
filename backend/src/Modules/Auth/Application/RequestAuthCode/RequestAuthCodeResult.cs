using MoneyTracker.Modules.Auth.Domain;

namespace MoneyTracker.Modules.Auth.Application.RequestAuthCode;

public sealed class RequestAuthCodeResult
{
    private RequestAuthCodeResult(bool isSuccess, AuthChallenge? challenge, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Challenge = challenge;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public AuthChallenge? Challenge { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static RequestAuthCodeResult Success(AuthChallenge challenge)
    {
        return new RequestAuthCodeResult(
            isSuccess: true,
            challenge: challenge,
            errorCode: null,
            errorMessage: null);
    }

    public static RequestAuthCodeResult Validation(string message)
    {
        return new RequestAuthCodeResult(
            isSuccess: false,
            challenge: null,
            errorCode: AuthErrors.ValidationError,
            errorMessage: message);
    }
}
