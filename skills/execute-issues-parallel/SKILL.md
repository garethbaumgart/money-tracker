---
name: execute-issues-parallel
description: Execute multiple GitHub issues concurrently using isolated git worktrees, one branch per issue, lane-aware startup rules, issue-specific implementation skills, tests, and one PR per issue. Use when the user asks to execute multiple issues in parallel (for example "Execute issues 5,6,7") or wants batch issue delivery with safe isolation.
---

# Execute Issues Parallel

## Overview

Run a multi-issue execution workflow with deterministic isolation and dependency-aware sequencing.
Use separate worktrees and open separate PRs by default for every issue in the request.

## Required Defaults

1. Create one git worktree per issue.
2. Create one branch per issue with prefix `codex/`.
3. Use `origin/main` as base unless the user specifies a different base branch.
4. Keep issue changes isolated; do not mix issue scopes in one branch.
5. Run issue-relevant tests before opening a PR.
6. Open one PR per issue.
7. Use `$github-pr` for PR description, risk summary, and review rounds.

## Worker Startup

For each issue worker:

1. Follow the canonical startup contract in `docs/dev-guide/workflow-catalog.md`.
2. Stay within lane unless the issue explicitly requires cross-lane edits.

## Skill Routing

1. Backend/API implementation: use `$backend-ddd-vertical-slice`.
2. Flutter UI/theming implementation: use `$flutter-ux-theming`.
3. UX-heavy issue requiring design options: use `$ux-mockup-explorer` before implementation.
4. Ambiguous issue text: use `$github-issue-refiner` before implementation.
5. PR creation/review loop: use `$github-pr`.

## Dependency Detection

Before creating workers:

1. Load each requested issue with `gh issue view <n>`.
2. Parse dependency cues from issue sections such as `Dependencies`, `Depends on`, and `Sequencing notes`.
3. Load each referenced same-repo numeric dependency issue with `gh issue view <n>` to capture current open/closed state.
4. Build a dependency graph across requested and external issues.
5. Mark each issue as:
   - `ready`: all dependencies already completed.
   - `in-set-blocked`: depends on another requested issue.
   - `externally-blocked`: depends on an issue outside the request that is not done.

Supported dependency format for automatic graphing: same-repo numeric issue references only (for example `#12`).
Treat non-standard references (cross-repo refs, plain links, or text without a numeric issue id) as manual dependencies and classify the issue as `externally-blocked` unless the dependency can be confirmed closed.

## Dependency Response Contract

If any blocked dependency exists, always report dependencies and present these options:

1. `Wave execution (Recommended)`: run ready issues first, then later waves as dependencies become satisfied.
2. `Ready-only now`: execute only ready issues; leave blocked issues unchanged.
3. `Custom order`: user supplies order; execute serially where dependency requires it.

Default when user says "Execute issues x,y,z" without preference: choose option 1.
Under option 1, execute all `ready` issues in the current wave and skip `externally-blocked` issues (do not poll/wait automatically); include skipped issues in the final blocked summary with the exact dependency that prevented execution.

## Worktree and Branch Naming

Use deterministic naming:

1. Worktree: `../<repo-name>-issue-<number>`.
2. Branch: `codex/issue-<number>-<slug>`.
3. Slug: lower-case, hyphenated issue title, shortened to practical length.

If worktree path or branch already exists, append `-v2`, `-v3`, and so on.

## Execution Loop

For each runnable issue (parallel inside each wave):

1. Fetch base refs (`git fetch origin`).
2. Create worktree and branch.
3. Implement using routed skill(s).
4. Run tests/checks for changed areas.
5. Confirm acceptance criteria are satisfied.
6. Open PR with `$github-pr`.
7. Continue review rounds until merge-ready unless the user explicitly says stop.

## Reporting Contract

Report these items:

1. Dependency summary and chosen option.
2. Worktree path and branch per issue.
3. Verification evidence per issue (tests/checks).
4. PR URL per issue.
5. Status per issue: `completed`, `in review`, `blocked`, or `failed`.
