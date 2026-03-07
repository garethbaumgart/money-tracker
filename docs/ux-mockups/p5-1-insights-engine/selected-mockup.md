# Premium Insights Dashboard — Selected UX

**Issue:** #74
**Decision:** Option A — Stacked Cards (all-in-one scroll, bar chart + ring gauge)
**Date:** 2026-03-07

## Rationale

Bar chart is familiar and easy to compare periods. Ring gauge is compact and visually distinct for budget health. Stacked cards allow natural scrolling through insights. Good visual hierarchy with all data on one scrollable screen.

## Selected Mockup

```
Premium user — full insights:
┌──────────────────────────────┐
│  < Back       Insights       │
├──────────────────────────────┤
│  Spending Trends (30d)       │
│  ┌──────────────────────┐    │
│  │  ██     This month   │    │
│  │  ██ ░░  Last month   │    │
│  │  ██ ░░               │    │
│  │  ██ ░░ ██            │    │
│  │  ██ ░░ ██ ░░ ██      │    │
│  │  Gro  Trn  Din  Ent  │    │
│  └──────────────────────┘    │
│                              │
│  Budget Health               │
│  ┌──────────────────────┐    │
│  │    ╭───────╮         │    │
│  │    │  78   │  Good   │    │
│  │    ╰───────╯         │    │
│  │  Adh: 85  Vel: 72    │    │
│  │  Bills: 80           │    │
│  └──────────────────────┘    │
│                              │
│  ⚠ Anomalies                 │
│  ┌──────────────────────┐    │
│  │ Dining  ↑ 68%        │    │
│  │ $340 vs $202 last mo │    │
│  └──────────────────────┘    │
└──────────────────────────────┘


Free user — paywall teaser:
┌──────────────────────────────┐
│  < Back       Insights       │
├──────────────────────────────┤
│  ░░░░░░░░░░░░░░░░░░░░░░░░   │
│  ░░ Spending Trends ░░░░░░   │
│  ░░░░░░ (blurred) ░░░░░░░   │
│  ░░░░░░░░░░░░░░░░░░░░░░░░   │
│                              │
│  🔒 Unlock Premium Insights  │
│                              │
│  ✓ Spending trend analysis   │
│  ✓ Budget health scoring     │
│  ✓ Anomaly detection         │
│                              │
│  [ Upgrade to Premium >>> ]  │
└──────────────────────────────┘
```

## Card Stack Order

1. **Spending Trends** — Grouped bar chart comparing current vs previous period by category
2. **Budget Health** — Ring gauge with composite score (0–100), sub-scores for adherence, velocity, bills
3. **Anomalies** — Alert cards for categories with significant spend changes (>30% deviation)

## Key UX Notes

- Single scrollable screen — no tabs, no hidden content
- Bar chart: grouped bars (current = solid, previous = hatched), top 4–6 categories
- Ring gauge: green (>70), amber (40–70), red (<40)
- Anomaly cards: sorted by severity, tappable for detail
- Free users see blurred preview with upgrade CTA
- Insights tab accessible from bottom nav or dashboard card
