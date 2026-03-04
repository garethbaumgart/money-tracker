---
name: github-issue-refiner
description: Refine rough, incomplete, or ambiguous GitHub issues into decision-complete engineering specs with clear scope, acceptance criteria, implementation approach, test plan, and risks. Use when a user asks to improve an issue, clarify requirements, split scope, or produce implementation-ready backlog items.
---

# Github Issue Refiner

## Overview

Turn fuzzy issue text into an implementation-ready specification.  
Produce a result that lets an engineer execute with minimal follow-up decisions.

## Workflow

1. Read the issue text and linked context.
2. Inspect codebase paths implied by the issue.
3. Resolve discoverable facts from the repo before asking for preferences.
4. Apply UX exploration gate criteria from `references/ux-exploration-gate.md`.
5. If UX gate is triggered, require `ux-mockup-explorer` output before implementation.
6. Rewrite the issue as a decision-complete spec.
7. Write acceptance criteria as `AC-<n>` and map each AC to required tests.
8. Add explicit assumptions where facts are missing.
9. Flag risks, dependencies, and rollout concerns.

## Output Contract

Produce these sections in order:

1. Problem Statement
2. Goals and Non-Goals
3. Scope
4. Current Behavior
5. Proposed Behavior
6. Technical Plan
7. API/Schema Contract Changes (if any)
8. UX Exploration Requirement (required/not required + reason)
9. Acceptance Criteria
10. Test Plan (required test matrix grouped by type)
11. Rollout and Monitoring
12. Risks and Mitigations
13. Dependencies
14. Open Questions (optional; only if unresolved and high impact)

## Quality Rules

1. Keep requirements testable and falsifiable.
2. Prefer concrete paths, interfaces, and data contracts over abstract wording.
3. Separate user intent from implementation assumptions.
4. Avoid shipping unresolved product decisions to implementation.
5. Keep output concise and executable.
6. If UX exploration is required, block implementation until selected option is recorded.
7. Acceptance criteria must be uniquely labeled (`AC-1`, `AC-2`, ...).
8. Test plan must include a required test matrix grouped by `Unit`, `Component`, `Integration`, `E2E`, and `Non-functional`.
9. Each test matrix row must include: test ID, mapped AC, scenario, expected assertion, suggested path/module, and whether the test is new or existing.
10. Every AC must map to at least one automated test, or explicitly state why it is manual-only.
11. Include at least one negative-path test for each behavior-changing AC unless not applicable, with reason.
12. Use exact section headings and numbering from `references/issue-refinement-template.md` so output can be validated mechanically.
13. For non-functional or exploratory ACs where direct 1:1 automation or negative-path tests are not meaningful, include a measurable verification probe/check and explicit rationale.
14. Required test matrix rows are automated tests only; manual checks belong in the `Manual-only ACs` subsection with justification.

## Scope Splitting Rules

If issue scope is too large:

1. Produce parent issue summary.
2. Propose child issues by vertical slice.
3. Give each child issue independent acceptance criteria.
4. Identify sequencing constraints and parallelizable tracks.

## Reference Files

1. Issue template and checklist:
- `references/issue-refinement-template.md`
2. UX gate criteria:
- `references/ux-exploration-gate.md`

Use that template when preparing final issue output.
