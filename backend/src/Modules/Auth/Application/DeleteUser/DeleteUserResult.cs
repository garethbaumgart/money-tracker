namespace MoneyTracker.Modules.Auth.Application.DeleteUser;

public sealed class DeleteUserResult
{
    public bool IsSuccess { get; private init; }
    public DateTimeOffset? ScheduledPurgeAtUtc { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static DeleteUserResult Success(DateTimeOffset scheduledPurgeAtUtc)
        => new() { IsSuccess = true, ScheduledPurgeAtUtc = scheduledPurgeAtUtc };

    public static DeleteUserResult Failure(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}
