namespace MoneyTracker.Modules.Auth.Application.ExportUserData;

public sealed class ExportUserDataResult
{
    public bool IsSuccess { get; private init; }
    public Dictionary<string, object>? Data { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ExportUserDataResult Success(Dictionary<string, object> data)
        => new() { IsSuccess = true, Data = data };

    public static ExportUserDataResult Failure(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
