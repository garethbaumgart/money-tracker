namespace MoneyTracker.Modules.Auth.Application.Logout;

public sealed class LogoutSessionResult
{
    private LogoutSessionResult(bool isSuccess, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public string? ErrorCode { get; }

    public static LogoutSessionResult Success() => new(isSuccess: true);
}
