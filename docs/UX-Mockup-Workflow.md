# UX Mockup Workflow and Storage Policy (v1)

## Purpose
Standardize how UX exploration artifacts are generated, reviewed, selected, and retained before Flutter implementation.

## When Required
UX mockup exploration is required for high-impact UX issues, including:
1. Onboarding or paywall changes.
2. New dashboard/navigation patterns.
3. New multi-step interaction flows.
4. Changes expected to influence activation, conversion, or retention.

## Artifact Standard
For each required issue, create this folder:

`docs/ux-mockups/<issue-id>-<slug>/`

Required contents:
1. `tokens.css`
2. `option-a/index.html`
3. `option-b/index.html`
4. `option-c/index.html`
5. `option-d/index.html`
6. `option-e/index.html`
7. `index.html` (single-page reviewer showing all options)
8. `decision.md`
9. `selected.txt` (only after decision)

## Option Design Rules
1. Options A-C must be divergent concepts, not visual clones.
2. Option D should represent the recommended direction candidate.
3. Option E should be a refined version of the selected direction.
4. HTML should reflect structure/flow intent for Flutter, not claim pixel-perfect parity.
5. `index.html` must make side-by-side review easy and include clear option labels.

## Decision Rule
1. Implementation is blocked for UX-required issues until `selected.txt` exists.
2. The selected option ID must be referenced in the GitHub issue.
3. The issue must record rationale in `decision.md`.
4. The single-page reviewer (`index.html`) should be linked in the issue for decision review.

## Retention Rule
1. Keep all options while decision is pending.
2. After decision, keep on `main`:
- `tokens.css`
- `index.html`
- `decision.md`
- selected option folder
- `selected.txt`
3. Move non-selected options to:
- `docs/ux-mockups/_archive/<issue-id>-<slug>/`

## Link to Skills and Controls
1. Issue gating: `skills/github-issue-refiner/SKILL.md`
2. Mockup generation: `skills/ux-mockup-explorer/SKILL.md`
3. Engineering policy: `docs/App-Build-GuideRails.md`
