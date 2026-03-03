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
7. Add explicit assumptions where facts are missing.
8. Flag risks, dependencies, and rollout concerns.

## Output Contract

Produce these sections in order:

1. Problem statement
2. Goals and non-goals
3. In scope and out of scope
4. Current behavior
5. Proposed behavior
6. Technical approach
7. API or schema changes (if any)
8. UX exploration requirement (required/not required + reason)
9. Test plan
10. Rollout and monitoring
11. Risks and mitigations
12. Open questions (only if unresolved and high impact)

## Quality Rules

1. Keep requirements testable and falsifiable.
2. Prefer concrete paths, interfaces, and data contracts over abstract wording.
3. Separate user intent from implementation assumptions.
4. Avoid shipping unresolved product decisions to implementation.
5. Keep output concise and executable.
6. If UX exploration is required, block implementation until selected option is recorded.

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
