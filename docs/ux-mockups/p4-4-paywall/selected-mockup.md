# Paywall Screen (Annual-First) — Selected UX

**Issue:** #82
**Decision:** Option A — Stacked Cards (full-screen paywall, feature list, dominant annual card)
**Date:** 2026-03-07

## Rationale

Full-screen stacked card layout is the industry-proven pattern for mobile subscription paywalls. Maximum space for value proposition with clear visual hierarchy — annual plan as the hero card. Feature list at top establishes value before price.

## Selected Mockup

```
Full-screen paywall:
┌──────────────────────────────┐
│  [X]     Go Premium          │
├──────────────────────────────┤
│                              │
│  Unlock everything:          │
│  ✓ Automatic bank sync      │
│  ✓ Spending insights         │
│  ✓ Budget health score       │
│  ✓ Anomaly detection         │
│                              │
│  ┌──────────────────────┐    │
│  │ ⭐ BEST VALUE         │    │
│  │ Annual    $59.99/yr  │    │
│  │ Just $4.99/month     │    │
│  │ ██████████████████   │    │
│  │ Save 58% vs monthly  │    │
│  └──────────────────────┘    │
│                              │
│  ┌──────────────────────┐    │
│  │ Monthly   $9.99/mo   │    │
│  └──────────────────────┘    │
│                              │
│  [ Start 14-Day Trial >>> ]  │
│                              │
│  Restore  |  Terms  |  Priv  │
└──────────────────────────────┘
```

## Visual Hierarchy

1. **Header** — Close [X] button + "Go Premium" title
2. **Value proposition** — Feature checklist (4 items)
3. **Annual card** — Dominant: star badge, border/shadow, savings callout, larger
4. **Monthly card** — Subdued: smaller, no decoration
5. **CTA** — Single "Start 14-Day Trial" button (applies to selected plan)
6. **Footer** — Restore, Terms, Privacy links

## Key UX Notes

- Annual card is pre-selected (annual-first per business requirement)
- Feature list uses checkmarks to reinforce "what you get"
- Savings callout ("Save 58%") anchors annual as better value
- Monthly card is visible but de-emphasized — no border, no badge
- Single CTA button for trial start (14-day free trial)
- Restore Purchases link in footer (App Store review requirement)
- Close button [X] always visible — never trap the user
- Screen is dismissible (swipe down or tap X)
