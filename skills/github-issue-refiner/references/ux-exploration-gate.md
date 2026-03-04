# UX Exploration Gate

Use this gate during issue refinement to decide whether UX mockups are required before implementation.

## Trigger Conditions
Mark UX exploration as `required` if any of these are true:

1. New onboarding or paywall flow.
2. New or significantly changed dashboard/navigation structure.
3. New multi-step form or interaction journey.
4. Changes that materially affect activation, conversion, or retention behavior.
5. Changes where layout/interaction tradeoffs are not obvious from text alone.

## Not Required Conditions
UX exploration is usually `not required` when:

1. UI change is minor copy/content only.
2. Pure bug fix with no meaningful interaction change.
3. Internal refactor with no UX behavior change.

## Required Artifacts When Triggered
1. Run `ux-mockup-explorer`.
2. Create a decision pack under:
- `docs/ux-mockups/<issue-id>-<slug>/`
3. Ensure the pack contains:
- `tokens.css`
- `option-a/index.html`
- `option-b/index.html`
- `option-c/index.html`
- `option-d/index.html`
- `option-e/index.html`
- `decision.md` with recommendation.
4. Include `selected.txt` (created after decision; may be absent before decision).
5. After decision, block implementation until `selected.txt` contains the chosen option.
6. Ensure the selected option ID is referenced in the refined issue.
