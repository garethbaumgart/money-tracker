# Bank Account Linking Flow — Selected UX

**Issue:** #65
**Decision:** Option D — Hybrid (dashboard banner + dedicated Bank screen)
**Date:** 2026-03-07

## Rationale

Maximizes activation via dashboard visibility while providing a scalable home for connection management. The thin banner is less intrusive than a full card, and the dedicated Bank screen solves discoverability and multi-bank management.

## Selected Mockup

```
Dashboard with thin banner (pre-link):
┌──────────────────────────────┐
│  MoneyTracker    [=] [Bell]  │
├──────────────────────────────┤
│ ┌────────────────────────┐   │
│ │ 🏦 Connect bank → Auto │   │
│ │ sync transactions  [>] │   │
│ └────────────────────────┘   │
│  Budget: $2,400 / $3,000     │
│  ████████████░░░░  80%       │
│                              │
│  Recent Transactions         │
│  > Groceries     -$45.20     │
├──────────────────────────────┤
│ Home  Budget  🏦Bank  More   │
└──────────────────────────────┘

Banner is thin, dismissible. Tapping leads
to the dedicated Bank screen.


Dedicated Bank Screen (empty state):
┌──────────────────────────────┐
│  Bank Accounts       [+Add]  │
├──────────────────────────────┤
│                              │
│  ┌─────────────────────┐     │
│  │  No banks linked     │    │
│  │                      │    │
│  │  Connect your bank   │    │
│  │  to auto-sync txns   │    │
│  │                      │    │
│  │  [ + Link Bank ]     │    │
│  └─────────────────────┘     │
│                              │
├──────────────────────────────┤
│ Home  Budget  🏦Bank  More   │
└──────────────────────────────┘


Pre-consent confirmation:
┌──────────────────────────────┐
│  < Back    Link Bank         │
├──────────────────────────────┤
│                              │
│     🏦                       │
│  We'll open your bank's     │
│  secure login page.         │
│                              │
│  ✓ Read-only access          │
│  ✓ 256-bit encryption        │
│  ✓ Revoke anytime            │
│                              │
│  [ Continue to Bank  >>> ]   │
│                              │
│  Powered by Basiq            │
└──────────────────────────────┘


Success state:
┌──────────────────────────────┐
│        Link Bank             │
├──────────────────────────────┤
│                              │
│          ✓                   │
│   Bank Connected!            │
│                              │
│   ANZ Everyday Account       │
│   ****4821                   │
│                              │
│  Transactions will sync      │
│  within a few minutes.       │
│                              │
│  [ Back to Dashboard >>> ]   │
└──────────────────────────────┘


Bank Screen (after linking):
┌──────────────────────────────┐
│  Bank Accounts       [+Add]  │
├──────────────────────────────┤
│  ┌──────────────────────┐    │
│  │ ANZ Everyday  ● Live │    │
│  │ ****4821             │    │
│  │ Last sync: 2 min ago │    │
│  └──────────────────────┘    │
│                              │
│  ┌──────────────────────┐    │
│  │ + Link another bank  │    │
│  └──────────────────────┘    │
├──────────────────────────────┤
│ Home  Budget  🏦Bank  More   │
└──────────────────────────────┘
```

## Key UX Notes

- Dashboard banner auto-dismisses after first bank linked
- Bank tab in bottom nav provides permanent home for connection management
- Scales naturally for multi-institution linking
- Pre-consent screen builds trust with security callouts
- Success screen confirms sync timing expectations
