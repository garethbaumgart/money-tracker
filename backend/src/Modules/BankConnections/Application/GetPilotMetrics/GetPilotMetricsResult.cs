using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;

public sealed class GetPilotMetricsResult
{
    private GetPilotMetricsResult(
        int periodDays,
        SyncMetrics? syncMetrics,
        LinkMetrics? linkMetrics,
        ConsentHealth? consentHealth,
        string? errorCode,
        string? errorMessage)
    {
        PeriodDays = periodDays;
        SyncMetrics = syncMetrics;
        LinkMetrics = linkMetrics;
        ConsentHealth = consentHealth;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public int PeriodDays { get; }
    public SyncMetrics? SyncMetrics { get; }
    public LinkMetrics? LinkMetrics { get; }
    public ConsentHealth? ConsentHealth { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    public bool IsSuccess => ErrorCode is null;

    public static GetPilotMetricsResult Success(
        int periodDays,
        SyncMetrics syncMetrics,
        LinkMetrics linkMetrics,
        ConsentHealth consentHealth)
    {
        return new GetPilotMetricsResult(periodDays, syncMetrics, linkMetrics, consentHealth, null, null);
    }

    public static GetPilotMetricsResult AccessDenied()
    {
        return new GetPilotMetricsResult(
            0, null, null, null,
            PilotMetricErrors.MetricsAccessDenied,
            "Admin access is required to view pilot metrics.");
    }
}

public sealed record SyncMetrics(
    double OverallSuccessRate,
    IReadOnlyCollection<RegionSyncMetric> ByRegion,
    IReadOnlyCollection<InstitutionSyncMetric> ByInstitution);

public sealed record RegionSyncMetric(string Region, double SuccessRate, double AvgLatencyMs);

public sealed record InstitutionSyncMetric(string Institution, double SuccessRate, double AvgLatencyMs);

public sealed record LinkMetrics(IReadOnlyCollection<InstitutionLinkMetric> ByInstitution);

public sealed record InstitutionLinkMetric(string Institution, int Attempted, int Successful);

public sealed record ConsentHealth(double AverageDurationDays, double ReConsentRate, double RevocationRate);
