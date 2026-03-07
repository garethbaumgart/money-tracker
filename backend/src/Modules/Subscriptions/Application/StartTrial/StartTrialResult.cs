using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.StartTrial;

public sealed class StartTrialResult
{
    private StartTrialResult(
        bool isSuccess,
        DateTimeOffset? trialExpiresAtUtc,
        string? errorCode,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        TrialExpiresAtUtc = trialExpiresAtUtc;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public DateTimeOffset? TrialExpiresAtUtc { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public static StartTrialResult Success(DateTimeOffset trialExpiresAtUtc)
    {
        return new StartTrialResult(true, trialExpiresAtUtc, null, null);
    }

    public static StartTrialResult AlreadyExists()
    {
        return new StartTrialResult(
            true,
            null,
            errorCode: null,
            errorMessage: null);
    }

    public static StartTrialResult Failed(string errorCode, string errorMessage)
    {
        return new StartTrialResult(false, null, errorCode, errorMessage);
    }
}
