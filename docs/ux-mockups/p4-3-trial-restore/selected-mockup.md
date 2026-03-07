# Trial & Restore Purchases — Selected UX

**Issue:** #79
**Decision:** Option B — Header Badge (TRIAL badge in app bar + bottom sheet for trial details)
**Date:** 2026-03-07

## Rationale

Badge provides persistent but subtle trial awareness without consuming dashboard space. Tapping the badge reveals full context including subscribe and restore CTAs in one sheet. The grace period modal clearly communicates the downgrade timeline. Supplemented with push notifications at 3-day and 1-day marks.

## Selected Mockup

```
Dashboard with subtle TRIAL badge:
┌──────────────────────────────┐
│  MoneyTracker  [TRIAL] [=]   │
├──────────────────────────────┤
│  Budget: $2,400 / $3,000     │
│  ████████████░░░░  80%       │
│                              │
│  Recent Transactions         │
│  > Groceries     -$45.20     │
└──────────────────────────────┘


Tapping "TRIAL" badge opens bottom sheet:
┌──────────────────────────────┐
│  ──────                      │
│  ⭐ Premium Trial             │
│  11 days remaining           │
│  ─────────────────────────── │
│  You're enjoying:            │
│  ✓ Bank sync                 │
│  ✓ Spending insights         │
│  ✓ Budget health score       │
│  ─────────────────────────── │
│  [ Subscribe Now     >>> ]   │
│  [ Restore Purchases     ]   │
└──────────────────────────────┘


Grace period modal (after trial expiry):
┌──────────────────────────────┐
│      Trial Ended             │
├──────────────────────────────┤
│                              │
│  Your 14-day trial has       │
│  ended. You still have       │
│  access for 3 more days.     │
│                              │
│  After that, you'll lose:    │
│  ✗ Bank sync                 │
│  ✗ Spending insights         │
│  ✗ Budget health score       │
│                              │
│  [ Subscribe Now >>> ]       │
│  Continue on Free            │
└──────────────────────────────┘
```

## Badge States

| State | Badge Text | Style |
|-------|-----------|-------|
| Active trial (>3 days) | `TRIAL` | Subtle, muted color |
| Expiring trial (<=3 days) | `TRIAL 3d` | Amber/warning color |
| Grace period | `EXPIRED` | Red |
| Paid subscriber | (no badge) | Hidden |
| Free (no trial) | (no badge) | Hidden |

## Key UX Notes

- Badge appears in app bar — always visible, never obstructive
- Bottom sheet includes both Subscribe and Restore CTAs
- Grace period (3 days post-expiry) with clear loss-aversion messaging
- Push notifications at 3-day and 1-day remaining marks
- Restore Purchases available in both bottom sheet and Settings
- Badge text updates dynamically with countdown in final 3 days
