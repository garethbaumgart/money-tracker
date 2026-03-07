# Alerting Rules

## API Error Rate

- **Threshold**: > 5% of requests returning 5xx status codes
- **Window**: 5-minute sliding window
- **Severity**: P2
- **Action**: Check `GET /admin/system-health` for module status, review recent deployments
- **Auto-recovery**: Alert clears when error rate drops below threshold

## Response Latency

- **Threshold**: p95 latency > 2 seconds
- **Window**: 5-minute sliding window
- **Severity**: P3
- **Action**: Review slow endpoints, check database query performance, verify infrastructure scaling
- **Escalation**: Escalate to P2 if p95 > 5 seconds

## Bank Sync Failure Rate

- **Threshold**: > 10% of sync operations failing
- **Window**: 1-hour sliding window
- **Severity**: P2
- **Action**: Check BankConnections module health, verify provider API status, review sync event logs
- **Escalation**: Escalate to P1 if failure rate > 50%

## Payment Webhook Lag

- **Threshold**: webhook processing latency > 5 minutes
- **Window**: rolling check every 5 minutes
- **Severity**: P2
- **Action**: Check Subscriptions module health, verify RevenueCat webhook delivery, review queue depth
- **Escalation**: Escalate to P1 if lag > 30 minutes

## Background Worker Health

- **Threshold**: worker heartbeat stale for > 5 minutes
- **Window**: continuous monitoring
- **Severity**: P3
- **Action**: Check worker status in system health endpoint, review worker logs, restart if necessary
- **Escalation**: Escalate to P2 if multiple workers are stale

## Module Health Degradation

- **Threshold**: any module reporting `degraded` status
- **Window**: continuous monitoring
- **Severity**: P3
- **Action**: Review module-specific health details, check infrastructure dependencies
- **Escalation**: Escalate to P2 if module reports `unhealthy`

## Module Health Failure

- **Threshold**: any module reporting `unhealthy` status
- **Window**: continuous monitoring
- **Severity**: P2
- **Action**: Immediate investigation, check module dependencies, review recent changes
- **Escalation**: Escalate to P1 if multiple modules are unhealthy

## System Health Endpoint Unreachable

- **Threshold**: `/admin/system-health` returns non-200 or times out
- **Window**: 3 consecutive failures (1-minute interval)
- **Severity**: P1
- **Action**: Check API availability, verify infrastructure status, initiate incident response

## Alert Routing

| Alert | Primary Channel | Secondary Channel |
|-------|----------------|-------------------|
| P1 alerts | PagerDuty (phone) | Slack #incidents |
| P2 alerts | PagerDuty (push) | Slack #incidents |
| P3 alerts | Slack #alerts | Email digest |
| P4 alerts | Slack #alerts | Weekly report |

## Silence and Maintenance Windows

- Scheduled maintenance: create maintenance window in alerting system before deployment
- False positive: silence for maximum 4 hours, create ticket to tune threshold
- Known issue: acknowledge alert and link to tracking ticket
