---
name: idea-intake
description: Turn raw product ideas into a clear, implementation-ready issue package by capturing intent, scope, acceptance criteria, dependencies, and execution mode. Use when user has an idea and asks for issue planning.
---

# Idea Intake

## When to use

Use for unshaped ideas, one-off feature requests, or “I have X that should do Y” prompts before opening/refining a GitHub issue.

## Workflow

1. Capture the idea in one sentence and the target outcome.
2. Ask only essential clarifying questions if any core decision is missing.
3. Identify:
   - affected lane (`backend`, `mobile`, `platform`)
   - expected feature depth (`small` / `medium` / `large`)
   - whether UX exploration is required
   - any blocking dependency or open decision
4. Draft an issue-ready plan with:
   - Proposed title
   - Problem and success criteria
   - Scope (in-scope, out-of-scope)
   - Draft acceptance criteria (`AC-1`, `AC-2`, `AC-3`)
   - Proposed merge mode (`draft` or `ai-review-loop`)
   - Suggested labels from the label strategy
5. Recommend next step:
   - if ambiguous or UX-heavy → refine with `$github-issue-refiner` then `$ux-mockup-explorer` when design is uncertain
   - if clear and narrow → hand off directly to `$github-issue-refiner` or issue execution flow

## Output contract

Produce:

- `Issue intent` (1–2 lines)
- `Problem statement`
- `User impact`
- `Acceptance criteria draft` (as numbered AC list)
- `Scope boundary` (`in scope` / `out of scope`)
- `Lane + dependencies`
- `Merge-ready mode`
- `Suggested next action`
