using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Application.GetPilotMetrics;

public sealed class GetPilotMetricsHandler(
    ISyncEventRepository syncEventRepository,
    ILinkEventRepository linkEventRepository,
    IBankConnectionRepository bankConnectionRepository,
    TimeProvider timeProvider)
{
    public async Task<GetPilotMetricsResult> HandleAsync(
        GetPilotMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var since = nowUtc.AddDays(-query.PeriodDays);

        var syncEvents = await syncEventRepository.GetByPeriodAsync(since, cancellationToken);
        var linkEvents = await linkEventRepository.GetByPeriodAsync(since, cancellationToken);

        // Apply optional region filter
        if (!string.IsNullOrWhiteSpace(query.Region))
        {
            var regionFilter = query.Region.Trim().ToUpperInvariant();
            syncEvents = syncEvents.Where(e => e.Region.Equals(regionFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
            linkEvents = linkEvents.Where(e => e.Region.Equals(regionFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        var syncMetrics = AggregateSyncMetrics(syncEvents);
        var linkMetrics = AggregateLinkMetrics(linkEvents);
        var consentHealth = await AggregateConsentHealthAsync(cancellationToken);

        return GetPilotMetricsResult.Success(query.PeriodDays, syncMetrics, linkMetrics, consentHealth);
    }

    private static SyncMetrics AggregateSyncMetrics(IReadOnlyCollection<SyncEvent> events)
    {
        var overallSuccessRate = events.Count > 0
            ? (double)events.Count(e => e.Outcome == EventOutcome.Success) / events.Count
            : 0.0;

        var byRegion = events
            .GroupBy(e => e.Region)
            .Select(g => new RegionSyncMetric(
                g.Key,
                g.Count() > 0 ? (double)g.Count(e => e.Outcome == EventOutcome.Success) / g.Count() : 0.0,
                g.Count() > 0 ? g.Average(e => e.DurationMs) : 0.0))
            .ToArray();

        var byInstitution = events
            .GroupBy(e => e.Institution)
            .Select(g => new InstitutionSyncMetric(
                g.Key,
                g.Count() > 0 ? (double)g.Count(e => e.Outcome == EventOutcome.Success) / g.Count() : 0.0,
                g.Count() > 0 ? g.Average(e => e.DurationMs) : 0.0))
            .ToArray();

        return new SyncMetrics(overallSuccessRate, byRegion, byInstitution);
    }

    private static LinkMetrics AggregateLinkMetrics(IReadOnlyCollection<LinkEvent> events)
    {
        var byInstitution = events
            .GroupBy(e => e.Institution)
            .Select(g => new InstitutionLinkMetric(
                g.Key,
                g.Count(),
                g.Count(e => e.Outcome == EventOutcome.Success)))
            .ToArray();

        return new LinkMetrics(byInstitution);
    }

    private async Task<ConsentHealth> AggregateConsentHealthAsync(CancellationToken cancellationToken)
    {
        var allConnections = await bankConnectionRepository.GetActiveConnectionsAsync(cancellationToken);

        // Also include non-active connections for revocation rate calculation
        // For now, we derive from what's available in the active connections repository
        // plus compute from all household connections.
        // We'll compute from the existing data available.

        if (allConnections.Count == 0)
        {
            return new ConsentHealth(0.0, 0.0, 0.0);
        }

        // Average consent duration in days (from creation to now for active, or creation to updated for terminal states)
        var nowUtc = timeProvider.GetUtcNow();
        var totalDurationDays = allConnections
            .Select(c => (nowUtc - c.CreatedAtUtc).TotalDays)
            .Average();

        // Re-consent rate and revocation rate require knowledge of all connections (not just active).
        // Since the repository only gives us active connections, we'll return 0 for these
        // until we have a method to fetch all connections. For now, this is a placeholder
        // that can be enhanced when the repository interface supports it.
        var reConsentRate = 0.0;
        var revocationRate = 0.0;

        return new ConsentHealth(
            Math.Round(totalDurationDays, 2),
            reConsentRate,
            revocationRate);
    }
}
