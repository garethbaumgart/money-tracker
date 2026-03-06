# Agent workflow quick start (idea → deployment)

This guide is for engineers using the Codex agent workflow to turn a rough idea into a production-ready change with minimal ambiguity.

## 0) Set context

Every session starts with a short declaration:

```text
Lane: backend | mobile | platform
Task type: idea-intake
Primary skills: $idea-intake
Merge-ready mode: draft
```

If you are unsure of lane initially, use `platform` for coordination and route to lane-specific workers after refinement.

## 1) Ideation and intake

Use `idea-intake` for raw ideas.

```text
$idea-intake
Idea: {{short description of outcome}}
Users: {{who it is for}}
Success metric: {{what confirms it works}}
Constraints: {{time, risk, dependencies, platform constraints}}
Decisions needed: {{unknowns}}

Return:
- issue intent and problem statement
- AC-1..AC-n
- lane + scope
- suggested labels
- recommended next action
```

Output should include:
- `AC-1..AC-n` in testable form
- one clear lane and boundary
- recommended follow-on skill (`$github-issue-refiner`, `$ux-mockup-explorer`, direct execution)

## 2) Issue refinement (required unless already implementation-ready)

Use `$github-issue-refiner` before implementation unless the idea is already precise.

```text
$github-issue-refiner
Source: {{paste idea-intake output}}

Return a full implementation-ready issue package with:
- scope, acceptance criteria, and test matrix
- risks and dependencies
- rollout and monitoring notes
```

If scope is too large, split into child issues before implementation.

## 3) UX check

If the issue is UX-heavy or behavior is uncertain:

```text
$github-issue-refiner
Mark whether UX exploration is required and why.
```

Then run:

```text
$ux-mockup-explorer
Generate options A-E and compare trade-offs.
Return a selected option and why.
```

Do not implement UI until UX option is selected.

## 4) Execute

Choose one of:

- Single issue:

```text
Lane: <backend|mobile|platform>
Task type: implementation
Primary skills: <$appropriate-skill>
Merge-ready mode: draft

Implement issue {{#}} end-to-end and open PR.
```

- Multiple issues:

```text
Lane: platform
Task type: parallel-issues
Primary skills: $execute-issues-parallel
Merge-ready mode: draft

Execute issues {{1,2,3}} with dependency-aware sequencing.
Create one PR per issue.
```

## 5) PR and verification gate

Use `$github-pr` before merging:

```text
Lane: platform
Task type: pr-only
Primary skills: $github-pr
Merge-ready mode: ai-review-loop

Prepare PR package for issue {{#}} with test evidence and risk summary.
```

Required checks before merge:
- run a full review immediately after PR creation
- if findings are identified, run a second full pre-merge review on latest head
- if no findings and risk is low, run a pre-merge checklist recheck
- issue status labels (`status:triage`, `status:refined`, `status:ready`, `status:in-progress`, `status:review`, `status:blocked`, `status:done`) and lane labels (`lane:backend`, `lane:mobile`, `lane:platform`, `lane:cross-cutting`, `lane:untriaged`) match current state
- AC evidence present
- required checks/check suite pass
- open blocking comments resolved

## 6) Deploy and close

After merge:
- confirm required deployment jobs run
- perform post-merge smoke checks
- keep issue labels at `status:done`
- close open dependencies and capture rollout notes
- add any follow-up cleanup issues immediately

## 7) Suggested prompt sequence (single copy-ready flow)

```text
Step 1: $idea-intake
Step 2: $github-issue-refiner
Step 3: $ux-mockup-explorer (only if flagged)
Step 4: implementation skill ($backend-ddd-vertical-slice | $flutter-ux-theming | $execute-issues-parallel)
Step 5: $github-pr
```

## Label policy used by this workflow

Keep one status label per issue:
- `status:triage`, `status:refined`, `status:ready`, `status:in-progress`, `status:review`, `status:blocked`, `status:done`

Keep one ownership label:
- `lane:backend`, `lane:mobile`, `lane:platform`, `lane:cross-cutting`, `lane:untriaged`

## Quick checks before PR

Use this checklist when changing automations, labels, or PR rules:
- workflow triggers reviewed (`on:` blocks)
- permissions/secrets remain minimal
- event-driven transitions still map to canonical `status:` and `lane:` label values
- failure/timeout behavior and fallback documented
