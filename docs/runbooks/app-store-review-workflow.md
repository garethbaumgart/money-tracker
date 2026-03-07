# App Store Review Workflow

## Monitoring Cadence

### Daily

- Check new app store reviews (iOS App Store Connect, Google Play Console)
- Review crash-free rate in crash reporting dashboard
- Check feedback submissions via `GET /admin/feedback-summary`

### Weekly

- Review aggregated NPS trends and week-over-week changes
- Triage new feedback items with priority >= High
- Review crash summary via `GET /admin/crash-summary`
- Respond to unanswered app store reviews older than 48 hours

### Monthly

- Publish internal feedback summary report
- Review priority distribution trends
- Update escalation criteria if needed

## Response Templates

### Positive Review (4-5 stars)

Thank you for the kind review! We are glad Money Tracker is helping you manage your finances. If you ever have suggestions or run into any issues, please use the in-app feedback form in Settings.

### Bug Report Review (1-3 stars)

Thank you for letting us know about this issue. We take bug reports seriously and our team is investigating. For the fastest resolution, please submit details through the in-app feedback form (Settings > Send Feedback) so we can gather device-specific information. We will follow up as soon as we have an update.

### Feature Request Review

Thank you for the suggestion! We are always looking for ways to improve Money Tracker. Your idea has been logged in our feedback system. While we cannot guarantee timelines, we prioritize features based on user demand and feasibility.

### Crash Report Review

We apologize for the crash you experienced. Our team monitors crash reports and is working on a fix. In the meantime, please ensure you are running the latest version of the app. If the issue persists, please submit a bug report through Settings > Send Feedback so we can gather additional diagnostic information.

## Escalation Criteria

### Immediate Escalation (within 1 hour)

- Crash-free rate drops below 99%
- Multiple 1-star reviews mentioning data loss
- Security-related reports
- Payment or subscription issues affecting multiple users

### Same-Day Escalation

- Crash-free rate drops below 99.5%
- Three or more similar bug reports in 24 hours
- Any review mentioning data corruption
- Feedback items auto-scored as Critical priority

### Standard Triage (next business day)

- Individual bug reports without data loss
- Feature requests
- General feedback with low-medium priority scores

## Feedback Triage Process

1. Review new feedback items in admin dashboard
2. Verify auto-computed priority score is appropriate
3. For Critical/High items: create support ticket and/or GitHub issue
4. Update feedback status to Triaged
5. For resolved items: update status to Resolved
6. For non-actionable items: update status to Dismissed with reason
