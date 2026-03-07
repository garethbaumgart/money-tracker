# Experiment Variants (Onboarding + Paywall) — Selected UX

**Issue:** #78
**Decision:** Option C — Progressive Disclosure (coaching tips + contextual paywall)
**Date:** 2026-03-07

## Rationale

Zero-friction onboarding (single tap) has the highest completion rate. Coaching tips guide users progressively without blocking their flow. Contextual paywall appears exactly when the user wants a premium feature, capitalizing on highest intent. The experiment framework should test this against guided wizard variants.

## Selected Mockup

```
First launch — single CTA:
┌──────────────────────────────┐
│                              │
│     💰                       │
│  MoneyTracker                │
│                              │
│  Track spending, budget      │
│  smarter, together.          │
│                              │
│  [ Get Started >>> ]         │
│                              │
└──────────────────────────────┘


Dashboard with coaching tips:
┌──────────────────────────────┐
│  MoneyTracker    [=] [Bell]  │
├──────────────────────────────┤
│ ┌────────────────────────┐   │
│ │ 💡 Tip: Add your first │   │
│ │ budget to get started  │   │
│ │ [ Create Budget > ] [x]│   │
│ └────────────────────────┘   │
│                              │
│  No budget yet               │
│                              │
│  Recent Transactions         │
│  (empty state)               │
│                              │
│  + Add Transaction           │
└──────────────────────────────┘


Contextual paywall (on premium feature tap):
┌──────────────────────────────┐
│  ──────                      │
│  🔒 Insights is Premium      │
│                              │
│  See where your money goes   │
│  with spending trends and    │
│  budget health scores.       │
│                              │
│  [ Unlock with Premium ]     │
│  [ Start 14-Day Trial  ]    │
│                              │
└──────────────────────────────┘
```

## Coaching Tip Sequence

| Trigger | Tip Content | CTA |
|---------|-------------|-----|
| First login | "Add your first budget to get started" | Create Budget |
| Budget created, no txns | "Add a transaction to track your spending" | Add Transaction |
| 3+ manual txns | "Link your bank to auto-sync" | Connect Bank |
| Bank linked, 7+ days | "Check your spending insights" | View Insights |

## Key UX Notes

- One-tap onboarding — user lands on dashboard immediately
- Coaching tips appear one at a time, dismissible with [x]
- Tips progress based on user actions, not time
- Contextual paywall is a bottom sheet — non-intrusive, stays in context
- Paywall shows feature-specific value prop (not generic)
- Experiment framework tests this variant (C) against guided wizard (A)
