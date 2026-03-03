---
name: github-pr
description: Prepare and shepherd pull requests from local changes with clear summary, behavior changes, risk analysis, test evidence, and disciplined review handling. Use when a user asks to prepare/open a PR, write a PR description, address review comments (including AI reviewers like Copilot, CodeRabbit, Sourcery), run multi-round review loops, or confirm merge readiness.
---

# GitHub PR

## Overview

Generate a high-signal PR package, then run review rounds until the PR is ready.
Focus on behavior, risk, verification evidence, and deterministic comment resolution.

## Workflow

1. Inspect branch status and changed files.
2. Group changes by behavior impact, not by commit order.
3. Identify user-visible changes, internal refactors, and risk areas.
4. Collect test evidence and list uncovered risk gaps.
5. Produce PR title, PR body, and reviewer checklist.
6. Open or update PR.
7. Run review loop until merge readiness criteria are met.

## Run Completion Gate

Do not end the skill run while either condition is true:

1. Any review/check is still in progress (for example CodeRabbit pending, Sourcery in progress, required checks pending).
2. Any actionable comment remains unresolved.

Only end the run when all are true:

1. No in-progress review/check statuses remain.
2. No unresolved actionable comments remain.
3. No new actionable comments have appeared during at least 2 poll intervals.

## Review Loop Protocol

1. Poll PR state for new reviews/comments and check results every 5 minutes.
2. Detect end of current round with this heuristic:
- All checks are complete (no pending required checks).
- No new AI-reviewer comments for a quiet window of at least 2 poll intervals (10 minutes).
- At least one signal from expected AI reviewers on current head commit when possible.
3. Build unresolved comment queue from review comments and issue comments.
4. Classify comments before acting:
- Actionable: inline review comments and concrete issue comments that request code/documentation changes.
- Non-actionable: "review in progress", rate-limit notices, summaries/walkthroughs, ads/tips, and informational status updates.
- Do not treat non-actionable comments as blockers.
5. For each unresolved actionable comment:
- Add a thumbs-up reaction first.
- Then either fix in code or reply with a technical rebuttal.
6. Never use "push to later feature" as the reason to skip a valid fix.
7. If rejecting a comment, provide specific evidence: incorrect assumption, constraint conflict, duplicate, or already addressed.
8. Push updates, post round summary, and request re-review.
9. Sleep 5 minutes and re-poll.
10. Repeat until run completion gate is satisfied.

Multiple rounds per PR are normal and expected.

## Output Contract

Output these sections:

1. PR title candidates (3)
2. Summary (what and why)
3. Behavior changes by area
4. Risk assessment
5. Test evidence
6. Migration/deployment notes
7. Post-merge checks
8. Reviewer focus points
9. Review round log (comment URL -> action taken)
10. Merge readiness status

## Quality Rules

1. Do not hide risk behind generic phrasing.
2. Separate confirmed test evidence from untested areas.
3. Link claims to changed files and observed behavior.
4. Keep summary concise; keep risk and testing explicit.
5. Prefer concrete reviewer instructions over broad requests.
6. Acknowledge every review comment before acting on it.
7. Resolve comments with code or hard technical reasoning, not vague deferral.

## Required Pre-PR Checks

1. Branch is up to date with target branch.
2. CI checks run locally or in pipeline where possible.
3. Security-sensitive changes are explicitly called out.
4. API contract changes include OpenAPI updates.
5. Migration risk is documented for DB or billing changes.

## Reference Files

1. PR template and checklist:
- `references/pr-template.md`
2. CLI commands for review polling, reactions, and response loop:
- `references/review-loop-commands.md`

Use these references when drafting PR output and running comment-resolution rounds.
