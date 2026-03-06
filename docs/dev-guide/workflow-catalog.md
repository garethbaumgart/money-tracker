# Workflow Catalog

## Quick Start (required)

- `Lane:` backend | mobile | platform
- `Task type:` idea-intake | issue-refinement | implementation | ux-design | parallel-issues | pr-only
- `Primary skills:` one or more `$skill-name`
- `Merge-ready mode:` draft | ai-review-loop

If you are onboarding a new engineer, start with [agent workflow quick start](./agent-workflow-quick-start.md) first.

Before implementation:

1. Read [index.md](./index.md)
2. Read the lane guide:
   - backend → `./backend.md`
   - mobile → `./mobile.md`
   - platform → `./platform.md`
3. For PR-facing work, read [verification.md](./verification.md)

Before implementation, if your scope touches automations, labels, event hooks, or PR rules, run the GitHub event workflow review checklist in the section below.

## Skill routing (single source of truth)

- Idea intake / issue creation → `$idea-intake`
- PR creation → `$github-pr`
- Backend/API implementation → `$backend-ddd-vertical-slice`
- Flutter UI/theming implementation → `$flutter-ux-theming`
- UX exploration/options → `$github-issue-refiner` then `$ux-mockup-explorer` (when requirements are ambiguous or UX-heavy); use only `$ux-mockup-explorer` directly when requirements are already precise.
- Issue refinement/spec drafting → `$github-issue-refiner`
- Multi-issue execution → `$execute-issues-parallel`
- Skill creation/update → `$skill-creator` (system path)
- Skill installation → `$skill-installer` (system path)

## Label strategy (canonical)

Use a small, consistent label set for GitHub issues:

- `phase:*` for roadmap grouping
- `type:*` for issue kind
- `lane:*` for ownership/execution boundary
- `status:*` for lifecycle state

Do not use labels for merge mode, priority, or risk.

- `merge-ready mode` is session metadata declared at task startup, not a GitHub label.

- `status:triage` → raw idea/triage
- `status:refined` → clarified enough for execution plan
- `status:ready` → approved and ready for implementation
- `status:in-progress` → actively being implemented
- `status:review` → needs review before merge
- `status:blocked` → waiting on dependency/decision
- `status:done` → completed and merged

- `lane:backend` / `lane:mobile` / `lane:platform`
- `lane:cross-cutting` when one issue touches multiple lanes
- `lane:untriaged` while automation infers lane
- `type:epic` / `type:feature`

Avoid ad hoc status labels and avoid stacking more than one `status:*` label on any issue.

## Lane guardrails

- backend: stay in `backend/**`
- mobile: stay in `mobile/**`
- platform: stay in `.github/**` and root tooling
- any cross-lane change requires explicit approval

## PR mode matrix

- Draft mode (default): PR package + verification evidence required; every PR runs an immediate full review pass after opening. A second pre-merge full pass is required only when the change is broad/high-concern or post-open review found findings; narrow clean PRs use a checklist pre-merge recheck.
- AI-review-loop mode: merge-ready loop, comment resolution, and AI reviewer metrics required, plus immediate post-open pass and the same high-concern pre-merge pass/recheck requirements.

## PR review gate (all modes)

- All PRs require a full review pass immediately after opening.
- Before merge, run another full review pass on the latest head when:
  - any actionable finding came from post-open review, or
  - the change is broad, cross-cutting, or otherwise high-concern.
- If post-open review had no actionable findings and the change is narrow and low-concern, a pre-merge checklist recheck is required.
- PR merge is blocked if unresolved actionable findings exist without an explicit technical rebuttal.

## Skills artifact table

- PR creation: `skills/github-pr/SKILL.md`
- Idea intake: `skills/idea-intake/SKILL.md`
- Backend: `skills/backend-ddd-vertical-slice/SKILL.md`
- Flutter/theming: `skills/flutter-ux-theming/SKILL.md`
- UX options: `skills/ux-mockup-explorer/SKILL.md`
- Issue refinement: `skills/github-issue-refiner/SKILL.md`
- Multi-issue execution: `skills/execute-issues-parallel/SKILL.md`
- Skill operations: `~/.codex/skills/.system/skill-creator/SKILL.md`, `~/.codex/skills/.system/skill-installer/SKILL.md`

## Copy-paste prompts (starter pack)

Use these templates to run the full flow quickly and consistently:

### 1) Idea intake

```text
$idea-intake
Here is a raw idea:

Idea: {{idea}}
Expected outcome: {{outcome}}
Target users: {{audience}}
Why now: {{business_reason}}
Hard constraints: {{time/budget/tech}}
Missing decisions: {{if_any}}

Please produce:
- issue intent and one-line problem statement
- AC-1..AC-n draft acceptance criteria
- lane, scope, and dependency assumptions
- suggested labels from the workflow label strategy
- recommended next action (refine, mockup, or execute)
```

### 2) Issue refinement

```text
$github-issue-refiner
Treat this as the implementation-ready spec source for issue {{#}}.

Source:
{{paste idea/intake output}}

Please generate full spec output in standard format, including scope split rules and test matrix.
```

### 3) UX-heavy path

```text
$github-issue-refiner
After acceptance and dependencies, tell me whether UX exploration is required. If yes, do not implement until mockups are selected.
```

```text
$ux-mockup-explorer
Create options A-E for this UX-heavy issue.
Return option summaries and a concise recommendation with trade-offs.
```

### 4) Issue execution

```text
Lane: {{backend|mobile|platform}}
Task type: {{implementation|parallel-issues|pr-only}}
Primary skills: {{$skill-name list}}
Merge-ready mode: {{draft|ai-review-loop}}

Single issue: implement issue {{#}} and open a PR.
Parallel execution: execute issues {{1,2,3}} with dependency-aware sequencing, one PR per issue.

After each PR is opened:
- run an immediate full review pass on that PR
- before merge, run another full review pass on latest head if post-open review has findings or risk is high; otherwise run a checklist recheck
```

## GitHub event workflow review (recommended)

Review `.github/workflows` YAML and repository automation configuration when your scope touches:
- issue lifecycle automation
- label mutations
- PR opening, checks, or gate behaviors
- release/build/deploy triggers

Checklist before merge:
1. Confirm triggers (`on:` blocks) still match intent.
2. Verify permissions and secrets are narrowly scoped.
3. Verify retry, timeout, and failure signaling are intentional.
4. Verify environment protections and required checks align with merge mode.
5. Record any added/changed/removed event trigger in PR notes.
