using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetRetentionCohorts;

public sealed class GetRetentionCohortsResult
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<CohortRetention> Cohorts { get; private init; } = [];
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static GetRetentionCohortsResult Success(IReadOnlyList<CohortRetention> cohorts) =>
        new()
        {
            IsSuccess = true,
            Cohorts = cohorts
        };

    public static GetRetentionCohortsResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
