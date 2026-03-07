using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Application.AllocateUser;

public sealed class AllocateUserResult
{
    private AllocateUserResult(
        ExperimentId? experimentId,
        string? experimentName,
        string? variantName,
        DateTimeOffset? allocatedAtUtc,
        string? errorCode,
        string? errorMessage)
    {
        ExperimentId = experimentId;
        ExperimentName = experimentName;
        VariantName = variantName;
        AllocatedAtUtc = allocatedAtUtc;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public ExperimentId? ExperimentId { get; }
    public string? ExperimentName { get; }
    public string? VariantName { get; }
    public DateTimeOffset? AllocatedAtUtc { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static AllocateUserResult Success(
        ExperimentId experimentId,
        string experimentName,
        string variantName,
        DateTimeOffset allocatedAtUtc)
    {
        return new AllocateUserResult(experimentId, experimentName, variantName, allocatedAtUtc, null, null);
    }

    public static AllocateUserResult Error(string errorCode, string errorMessage)
    {
        return new AllocateUserResult(null, null, null, null, errorCode, errorMessage);
    }
}
