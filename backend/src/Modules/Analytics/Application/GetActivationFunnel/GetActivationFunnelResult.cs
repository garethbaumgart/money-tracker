using MoneyTracker.Modules.Analytics.Domain;

namespace MoneyTracker.Modules.Analytics.Application.GetActivationFunnel;

public sealed class GetActivationFunnelResult
{
    public bool IsSuccess { get; private init; }
    public int PeriodDays { get; private init; }
    public string Platform { get; private init; } = "all";
    public string Region { get; private init; } = "all";
    public int TotalUsers { get; private init; }
    public IReadOnlyCollection<FunnelStage> Stages { get; private init; } = [];
    public IReadOnlyCollection<CohortSummary> Cohorts { get; private init; } = [];
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static GetActivationFunnelResult Success(
        int periodDays,
        string platform,
        string region,
        int totalUsers,
        IReadOnlyCollection<FunnelStage> stages,
        IReadOnlyCollection<CohortSummary> cohorts) =>
        new()
        {
            IsSuccess = true,
            PeriodDays = periodDays,
            Platform = platform,
            Region = region,
            TotalUsers = totalUsers,
            Stages = stages,
            Cohorts = cohorts
        };

    public static GetActivationFunnelResult Failure(string errorCode, string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
}
