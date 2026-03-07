namespace MoneyTracker.Modules.Analytics.Application.RecordEvent;

public sealed class RecordEventResult
{
    public bool IsSuccess { get; private init; }
    public int AcceptedCount { get; private init; }
    public int DuplicateCount { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static RecordEventResult Success(int acceptedCount, int duplicateCount) =>
        new()
        {
            IsSuccess = true,
            AcceptedCount = acceptedCount,
            DuplicateCount = duplicateCount
        };

    public static RecordEventResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
