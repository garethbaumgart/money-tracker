namespace MoneyTracker.Modules.Notifications.Domain;

public sealed class NotificationDispatchResult
{
    private NotificationDispatchResult(bool isSuccess, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static NotificationDispatchResult Success()
    {
        return new NotificationDispatchResult(true, errorCode: null, errorMessage: null);
    }

    public static NotificationDispatchResult Failure(string code, string message)
    {
        return new NotificationDispatchResult(false, code, message);
    }
}
