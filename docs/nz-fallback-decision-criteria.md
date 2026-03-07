# NZ Fallback Decision Criteria

This document defines the concrete thresholds and alerting rules used to evaluate whether the Basiq provider is sufficient for New Zealand bank connections, or whether an alternative adapter (e.g., Akahu) should be built.

## Decision Framework

The pilot runs for a minimum of 30 days with NZ bank connections enabled via Basiq. Metrics are collected automatically via the `GET /admin/pilot-metrics` endpoint and evaluated against the thresholds below.

## Decision Thresholds

### Sync Reliability

| Metric | Threshold | Action if Breached |
|---|---|---|
| NZ sync success rate | < 95% over 30-day window | Build Akahu adapter |
| NZ sync success rate | < 90% over 7-day window | Escalate immediately; begin Akahu investigation |
| AU sync success rate (baseline) | < 95% over 30-day window | Investigate Basiq platform issues (not NZ-specific) |

### Institution Coverage

| Metric | Threshold | Action if Breached |
|---|---|---|
| NZ institution coverage | < 5 major NZ banks successfully linked | Build Akahu adapter |
| NZ institution link failure rate | > 20% for any major institution | Investigate institution-specific issues |

### Sync Latency

| Metric | Threshold | Action if Breached |
|---|---|---|
| NZ average sync latency | > 30 seconds | Investigate; consider Akahu if persistent |
| NZ P95 sync latency | > 60 seconds | Escalate to Basiq support |
| AU average sync latency (baseline) | > 30 seconds | Investigate Basiq platform issues |

### Consent Health

| Metric | Threshold | Action if Breached |
|---|---|---|
| NZ consent revocation rate | > 10% over 30-day window | Investigate UX or provider issues |
| NZ re-consent rate | > 15% over 30-day window | Investigate consent duration issues |

## Minimum Sample Size

For statistical significance, the following minimum sample sizes are required before making a decision:

- **Sync events**: Minimum 100 NZ sync attempts across at least 5 distinct institutions
- **Link events**: Minimum 50 NZ link attempts across at least 3 distinct institutions
- **Time period**: Minimum 30 days of continuous data collection

If minimum sample sizes are not met within 60 days, escalate to product team for manual evaluation.

## Alerting Rules

The following alerting thresholds trigger notifications when breached:

### Critical Alerts (immediate action required)

- NZ sync success rate < 95% over a rolling 7-day window
- Any NZ institution with 0% sync success rate over 24 hours (with > 5 attempts)
- NZ institution link failure rate > 20% over a rolling 7-day window

### Warning Alerts (investigation required)

- Average sync latency > 30 seconds for any region over a rolling 24-hour window
- NZ sync success rate between 95-97% over a rolling 7-day window (trending toward threshold)
- Consent revocation rate > 10% for NZ connections over a rolling 30-day window

### Informational Alerts

- New NZ institution first successfully linked
- NZ daily sync volume drops below 10 events (potential coverage issue)
- Pilot sample size milestone reached (50%, 100% of minimum)

## Major NZ Banks (Target Coverage)

The following institutions represent the minimum coverage target for NZ:

1. ANZ New Zealand
2. ASB Bank
3. BNZ (Bank of New Zealand)
4. Kiwibank
5. Westpac New Zealand

## Decision Timeline

- **Week 1-2**: Ramp-up period; collect baseline data, no decisions
- **Week 3-4**: Evaluate trends; flag any critical threshold breaches
- **Week 5-8**: Full evaluation period; make go/no-go decision on Akahu adapter
- **Beyond Week 8**: If minimum sample sizes not met, escalate for manual review

## Endpoint Reference

Metrics are available via:

```
GET /admin/pilot-metrics?periodDays=30&region=NZ
```

Query parameters:
- `periodDays` (default: 30) - Number of days to aggregate
- `region` (optional) - Filter by region code (e.g., "NZ", "AU")

Response includes sync success rates, latency averages, institution coverage, and consent health metrics.
