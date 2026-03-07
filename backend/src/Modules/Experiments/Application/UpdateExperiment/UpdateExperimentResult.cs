namespace MoneyTracker.Modules.Experiments.Application.UpdateExperiment;

public sealed class UpdateExperimentResult
{
    private UpdateExperimentResult(string? errorCode, string? errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static UpdateExperimentResult Success()
    {
        return new UpdateExperimentResult(null, null);
    }

    public static UpdateExperimentResult Error(string errorCode, string errorMessage)
    {
        return new UpdateExperimentResult(errorCode, errorMessage);
    }
}
