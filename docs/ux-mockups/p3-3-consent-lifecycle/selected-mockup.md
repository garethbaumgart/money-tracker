# Consent Failure & Re-consent — Selected UX

**Issue:** #67
**Decision:** Option A — Inline Badges (green/amber/red status on bank connection cards)
**Date:** 2026-03-07

## Rationale

Status badges on the Bank screen cards are universally understood (green = active, amber = expiring, red = expired/revoked). The reconnect CTA appears right where the user sees the problem. Supplemented with push notifications for expiry warnings to proactively reach users.

## Selected Mockup

```
Bank Screen with mixed connection states:
┌──────────────────────────────┐
│  Bank Accounts       [+Add]  │
├──────────────────────────────┤
│ ⚠ 1 connection needs action  │
├──────────────────────────────┤
│  ┌──────────────────────┐    │
│  │ ANZ Everyday         │    │
│  │ ● Active  Exp: 82d  │    │
│  │ Last sync: 5 min ago │    │
│  └──────────────────────┘    │
│                              │
│  ┌──────────────────────┐    │
│  │ Westpac Savings      │    │
│  │ 🔴 Expired           │    │
│  │ Consent expired 3d   │    │
│  │ [ Reconnect >>> ]    │    │
│  └──────────────────────┘    │
├──────────────────────────────┤
│ Home  Budget  🏦Bank  More   │
└──────────────────────────────┘


Expiring soon state (7-day warning):
┌──────────────────────────────┐
│  ┌──────────────────────┐    │
│  │ ANZ Everyday         │    │
│  │ 🟡 Expiring in 5d    │    │
│  │ Renew to keep sync   │    │
│  │ [ Renew Now ]        │    │
│  └──────────────────────┘    │
└──────────────────────────────┘
```

## Badge States

| State | Badge | Color | CTA |
|-------|-------|-------|-----|
| Active (>7 days) | `● Active` | Green | None |
| Expiring (<=7 days) | `🟡 Expiring in Xd` | Amber | Renew Now |
| Expired | `🔴 Expired` | Red | Reconnect |
| Revoked | `🔴 Revoked` | Red | Reconnect |
| Failed | `🔴 Failed` | Red | Retry |

## Key UX Notes

- Warning banner at top counts connections needing action
- Badge color coding: green (active), amber (expiring soon), red (expired/revoked/failed)
- Reconnect CTA inline on affected cards — no extra navigation
- Supplement with push notifications at 7-day and 3-day expiry marks
- Renew flow reuses the same Basiq consent session creation
