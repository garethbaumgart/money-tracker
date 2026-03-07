# In-App Feedback & NPS — Selected UX

**Issue:** #86
**Decision:** Option C — Contextual Prompts (star rating after actions + tappable NPS row in settings)
**Date:** 2026-03-07

## Rationale

Contextual prompts capture feedback at the moment of highest relevance, leading to higher quality responses. Progressive disclosure (star rating first, detail optional) balances low friction with rich data collection. Full-width NPS number row maintains industry-standard 0–10 scale while being tappable on mobile.

## Selected Mockup

```
After completing an action (e.g., adding transaction):
┌──────────────────────────────┐
│  ──────                      │
│  How was that?               │
│                              │
│  ★ ★ ★ ★ ★                  │
│                              │
│  [ Tell us more ]   [ Done ] │
└──────────────────────────────┘


"Tell us more" expands to full form:
┌──────────────────────────────┐
│  < Back     Feedback         │
├──────────────────────────────┤
│  You rated: ★ ★ ★ ☆ ☆       │
│                              │
│  Category:                   │
│  [Bug] [Feature] [General]   │
│                              │
│  ┌──────────────────────┐    │
│  │ What could be better?│    │
│  │                      │    │
│  └──────────────────────┘    │
│                              │
│  [ Submit >>> ]              │
└──────────────────────────────┘


NPS as full-width tappable number row:
┌──────────────────────────────┐
│  ──────                      │
│  How likely to recommend     │
│  MoneyTracker to a friend?   │
│                              │
│  ┌─┬─┬─┬─┬─┬─┬─┬─┬─┬─┬──┐  │
│  │0│1│2│3│4│5│6│7│8│9│10│  │
│  └─┴─┴─┴─┴─┴─┴─┴─┴─┴─┴──┘  │
│  Unlikely       Very likely  │
│                              │
│  Why? (optional)             │
│  ┌──────────────────────┐    │
│  │                      │    │
│  └──────────────────────┘    │
│  [ Submit ]     [ Skip ]     │
└──────────────────────────────┘
```

## Contextual Prompt Triggers

| Trigger | Condition | Frequency |
|---------|-----------|-----------|
| Post-action | After adding 5th transaction | Once |
| Post-action | After first budget review | Once |
| Post-action | After first insight view | Once |
| NPS survey | After 14 days of usage | Once per quarter |
| NPS survey | After subscription renewal | Once per cycle |

## Key UX Notes

- Star rating prompt is a bottom sheet — one-tap dismissible
- "Done" sends the star rating alone (no extra steps required)
- "Tell us more" is optional progressive disclosure
- Category chips (Bug/Feature/General) for routing feedback
- NPS uses standard 0–10 scale (industry-benchmarkable)
- Full-width number row ensures tappable targets on mobile
- NPS includes optional "Why?" text field
- Settings fallback: "Send Feedback" row in Settings for deliberate feedback
- Rate limiting: max 1 contextual prompt per session, not on every action
