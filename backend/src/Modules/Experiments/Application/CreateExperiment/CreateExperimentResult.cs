namespace MoneyTracker.Modules.Experiments.Application.CreateExperiment;

public sealed class CreateExperimentResult
{
    private CreateExperimentResult(
        Guid? experimentId,
        string? errorCode,
        string? errorMessage)
    {
        ExperimentId = experimentId;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Guid? ExperimentId { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static CreateExperimentResult Success(Guid experimentId)
    {
        return new CreateExperimentResult(experimentId, null, null);
    }

    public static CreateExperimentResult Error(string errorCode, string errorMessage)
    {
        return new CreateExperimentResult(null, errorCode, errorMessage);
    }
}
