namespace MoneyTracker.Modules.Experiments.Application.GetActiveAllocations;

public sealed class GetActiveAllocationsResult
{
    private GetActiveAllocationsResult(
        IReadOnlyList<AllocationDto>? allocations,
        string? errorCode,
        string? errorMessage)
    {
        Allocations = allocations;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public IReadOnlyList<AllocationDto>? Allocations { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static GetActiveAllocationsResult Success(IReadOnlyList<AllocationDto> allocations)
    {
        return new GetActiveAllocationsResult(allocations, null, null);
    }

    public static GetActiveAllocationsResult Error(string errorCode, string errorMessage)
    {
        return new GetActiveAllocationsResult(null, errorCode, errorMessage);
    }
}

public sealed record AllocationDto(
    Guid ExperimentId,
    string ExperimentName,
    string VariantName,
    DateTimeOffset AllocatedAtUtc);
