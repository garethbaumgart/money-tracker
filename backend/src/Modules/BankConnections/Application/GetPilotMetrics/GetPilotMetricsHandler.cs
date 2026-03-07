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
        var allConnections = await bankConnectionRepository.GetAllConnectionsAsync(cancellationToken);

        if (allConnections.Count == 0)
        {
            return new ConsentHealth(0.0, 0.0, 0.0);
        }

        // Average consent duration in days (from creation to now for active, or creation to updated for terminal states)
        var nowUtc = timeProvider.GetUtcNow();
        var totalDurationDays = allConnections
            .Select(c => c.Status is BankConnectionStatus.Revoked or BankConnectionStatus.Failed
                ? (c.UpdatedAtUtc - c.CreatedAtUtc).TotalDays
                : (nowUtc - c.CreatedAtUtc).TotalDays)
            .Average();

        // Revocation rate: proportion of all connections that are currently revoked.
        var revokedCount = allConnections.Count(c => c.Status == BankConnectionStatus.Revoked);
        var revocationRate = (double)revokedCount / allConnections.Count;

        // Re-consent rate requires historical state-transition tracking (e.g. a connection
        // that moved from Expired/Revoked back to Active). The current domain model does
        // not persist state-change history, so this metric cannot be computed yet.
        // TODO(#68): Implement connection state history to enable re-consent rate tracking.
        var reConsentRate = 0.0;

        return new ConsentHealth(
            Math.Round(totalDurationDays, 2),
            reConsentRate,
            Math.Round(revocationRate, 4));
    }
}
