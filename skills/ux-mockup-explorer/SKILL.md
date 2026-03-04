---
name: ux-mockup-explorer
description: Generate UX exploration artifacts as raw HTML option packs for product decision-making before Flutter implementation. Use when an issue has significant UX impact (onboarding, paywall, dashboard/navigation changes, new multi-step flows, high-risk interaction changes) and stakeholders need comparable options before coding.
---

# Ux Mockup Explorer

## Overview

Create a decision-ready UX option pack with standardized storage and selection artifacts.
Optimize for fast choice-making, not pixel-perfect Flutter parity.

## Workflow

1. Read issue context, user goals, constraints, and target device assumptions.
2. Define key UX decision dimensions (information hierarchy, navigation, input flow, feedback model).
3. Generate five artifacts:
- Option A: concept 1 (divergent)
- Option B: concept 2 (divergent)
- Option C: concept 3 (divergent)
- Option D: selected-direction candidate
- Option E: refined selected-direction version
4. Keep all options in raw HTML/CSS with shared semantic token file.
5. Create a one-page review surface at `index.html` that shows all options in one place (via embeds or direct links), includes recommendation context, and makes owner approval easy.
6. Build `decision.md` with option summaries, tradeoffs, recommendation, and selection status.
7. Mark implementation as blocked until `selected.txt` exists with chosen option id.

## Storage Contract

Store artifacts here:

1. `docs/ux-mockups/<issue-id>-<slug>/tokens.css`
2. `docs/ux-mockups/<issue-id>-<slug>/option-a/index.html`
3. `docs/ux-mockups/<issue-id>-<slug>/option-b/index.html`
4. `docs/ux-mockups/<issue-id>-<slug>/option-c/index.html`
5. `docs/ux-mockups/<issue-id>-<slug>/option-d/index.html`
6. `docs/ux-mockups/<issue-id>-<slug>/option-e/index.html`
7. `docs/ux-mockups/<issue-id>-<slug>/index.html` (single-page reviewer)
8. `docs/ux-mockups/<issue-id>-<slug>/decision.md`
9. `docs/ux-mockups/<issue-id>-<slug>/selected.txt` (created only after decision)

## Quality Rules

1. Make options structurally different, not only visual variants.
2. Keep copy realistic and tied to issue context.
3. Keep token names semantic and aligned with Flutter theme roles.
4. Keep options lightweight and inspectable in browser without build tooling.
5. Explicitly state recommendation and tradeoffs in `decision.md`.
6. Ensure `index.html` is fast to scan and clearly presents all options for approval.

## Flutter Parity Rules

1. Match hierarchy, spacing rhythm, and control types expected in Flutter.
2. Do not claim pixel-perfect parity with Flutter rendering.
3. Include responsive behavior for compact and expanded widths.
4. Use states for loading, empty, and error where relevant.

## Retention Rules

1. Keep all options until a decision is made.
2. After selection, keep:
- `tokens.css`
- `index.html`
- `decision.md`
- selected option folder
- `selected.txt`
3. Move non-selected options to:
- `docs/ux-mockups/_archive/<issue-id>-<slug>/`

## Reference Files

1. Decision pack template:
- `references/ux-mockup-decision-template.md`
