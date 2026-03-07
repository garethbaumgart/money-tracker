using MoneyTracker.Modules.Notifications.Domain;

namespace MoneyTracker.Modules.Notifications.Application.RegisterDeviceToken;

public sealed class RegisterDeviceTokenResult
{
    private RegisterDeviceTokenResult(DeviceToken? token, string? errorCode, string? errorMessage)
    {
        Token = token;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public DeviceToken? Token { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Token is not null;

    public static RegisterDeviceTokenResult Success(DeviceToken token)
    {
        return new RegisterDeviceTokenResult(token, errorCode: null, errorMessage: null);
    }

    public static RegisterDeviceTokenResult Validation(string code, string message)
    {
        return new RegisterDeviceTokenResult(token: null, code, message);
    }
}
