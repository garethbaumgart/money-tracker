## Issue
- Issue ID: #4
- Title: [P1-3] Build Flutter app shell with Material 3 theme and token foundation
- Goal: Select a base navigation and shell information hierarchy before Flutter implementation.

## Decision Dimensions
- Dimension 1: Navigation model clarity across compact and expanded widths
- Dimension 2: Priority hierarchy for shared-budget decision making
- Dimension 3: Ease of translating structure into tokenized Flutter components

## Option Summary
| Option | Intent | Strengths | Tradeoffs |
|---|---|---|---|
| A | Bottom tab command center with summary-first home | Familiar mobile pattern, fast one-thumb navigation, simple Flutter mapping | Expanded-width behavior is weaker and less desktop-friendly |
| B | Rail workspace for stronger desktop memory | Strong scan speed on wide layouts, clear IA separation by destination | Feels heavy on compact phones and can over-index on navigation chrome |
| C | Timeline-first journal shell | Excellent for transaction triage and recency workflows | Budget confidence and planning context are less prominent |
| D | Hybrid candidate balancing confidence plus activity | Good balance of summary and action, works for both planning and triage | Needs refined hierarchy to avoid equal visual weight across sections |
| E | Refined hybrid with explicit priority ladder | Best decision flow (confidence -> checklist -> activity), clean responsive behavior, direct token mapping for Flutter themes | Slightly denser than A on first load and requires disciplined spacing in implementation |

## Recommendation
- Recommended option: `option-e`
- Why: It gives the strongest default decision path for a shared budget app while staying implementation-friendly for Material 3 component theming.
- Key risk: Content density can feel high on small devices if spacing and typography are not tuned.
- Mitigation: Use compact spacing tokens selectively and collapse secondary cards behind progressive disclosure on small widths.

## Selection Status
- Selected option: `option-e`
- Decision date: 2026-03-04
- Decider: Repository owner (approved in chat)
- Notes: Option E is approved and selected for implementation.

## Review Artifact
- Single-page option viewer:
  - `docs/ux-mockups/4-app-shell-theme-foundation/index.html`

## Implementation Gate
- [x] `selected.txt` created with chosen option id
- [x] Link to selected HTML mockup
  - `docs/ux-mockups/4-app-shell-theme-foundation/option-e/index.html`
- [x] Issue updated with selected option rationale
