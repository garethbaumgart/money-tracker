# Incident Response Runbook

## Severity Levels

### P1 - Critical

- Complete service outage or data loss
- All users affected
- Revenue-impacting failure (payment processing down)
- Response time: immediate (within 15 minutes)
- Resolution target: 1 hour

### P2 - High

- Major feature unavailable (bank sync, transaction creation)
- Significant percentage of users affected (>25%)
- Degraded performance (p95 > 5s)
- Response time: within 30 minutes
- Resolution target: 4 hours

### P3 - Medium

- Minor feature unavailable (insights, analytics)
- Small percentage of users affected (<25%)
- Intermittent errors (error rate 5-15%)
- Response time: within 2 hours
- Resolution target: 24 hours

### P4 - Low

- Cosmetic issues or minor bugs
- Workaround available
- Non-user-facing system degradation
- Response time: next business day
- Resolution target: 1 week

## Escalation Matrix

| Severity | First Responder | Escalation (30 min) | Executive Notification |
|----------|----------------|---------------------|----------------------|
| P1       | On-call engineer | Engineering lead | CTO + stakeholders |
| P2       | On-call engineer | Engineering lead | Engineering manager |
| P3       | Assigned engineer | Team lead | None |
| P4       | Assigned engineer | None | None |

## Response Procedure

### 1. Acknowledge

- Confirm the incident in the alerting channel
- Assign an incident commander
- Create an incident tracking thread

### 2. Assess

- Check `GET /admin/system-health` for module status
- Review error rate and latency metrics
- Identify affected users and scope
- Determine severity level

### 3. Communicate

- Post initial status update within 15 minutes of acknowledgement
- Update stakeholders at regular intervals (P1: every 15 min, P2: every 30 min)
- Use the communication templates below

### 4. Mitigate

- Apply immediate mitigation (rollback, feature flag, scaling)
- Document all actions taken with timestamps
- Verify mitigation effectiveness via health endpoint

### 5. Resolve

- Deploy permanent fix
- Verify resolution via monitoring
- Update status page
- Close the incident

## Communication Templates

### Initial Notification

```
INCIDENT: [Brief description]
SEVERITY: P[1-4]
STATUS: Investigating
IMPACT: [Who is affected and how]
STARTED: [Timestamp UTC]
NEXT UPDATE: [Timestamp UTC]
```

### Status Update

```
INCIDENT UPDATE: [Brief description]
STATUS: [Investigating | Identified | Mitigating | Resolved]
CURRENT IMPACT: [Updated impact assessment]
ACTIONS TAKEN: [What has been done]
NEXT STEPS: [What will be done next]
NEXT UPDATE: [Timestamp UTC]
```

### Resolution Notification

```
INCIDENT RESOLVED: [Brief description]
SEVERITY: P[1-4]
DURATION: [Total duration]
ROOT CAUSE: [Brief root cause]
RESOLUTION: [What was done to fix it]
POST-MORTEM: [Scheduled date/link]
```

## Post-Mortem Template

### Incident Summary

- Date/time (UTC)
- Duration
- Severity
- Impact (users affected, revenue impact)

### Timeline

| Time (UTC) | Event |
|------------|-------|
| HH:MM      | [What happened] |

### Root Cause

[Detailed technical explanation of what caused the incident]

### Contributing Factors

- [Factor 1]
- [Factor 2]

### Resolution

[What was done to resolve the incident]

### Lessons Learned

#### What went well

- [Item]

#### What could be improved

- [Item]

### Action Items

| Action | Owner | Due Date | Status |
|--------|-------|----------|--------|
| [Action item] | [Name] | [Date] | Open |

## Database Backup Procedure

### Automated Backups

- Full backup: daily at 02:00 UTC
- Incremental backup: every 6 hours
- Retention: 30 days for daily, 7 days for incremental
- Storage: encrypted, geo-redundant storage

### Manual Backup

1. Trigger manual backup via cloud provider console
2. Verify backup completion and integrity
3. Record backup ID and timestamp

### Restore Procedure

1. Identify the backup point to restore from
2. Create a new database instance from backup
3. Verify data integrity on the restored instance
4. Update connection strings to point to restored instance
5. Verify application connectivity and functionality

### Backup Verification

- Monthly restore test to verify backup integrity
- Automated integrity checks after each backup
- Alert on backup failure or size anomaly
