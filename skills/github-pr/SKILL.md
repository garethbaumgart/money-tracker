---
name: github-pr
description: Prepare and shepherd pull requests from local changes with clear summary, behavior changes, risk analysis, test evidence, and disciplined review handling. Use when a user asks to prepare/open a PR, write a PR description, address review comments (including AI reviewers like Copilot, CodeRabbit, Sourcery), run multi-round review loops, or confirm merge readiness.
---

# GitHub PR

## Overview

Generate a high-signal PR package, run an immediate full review, and continue review until merge conditions are met.
Focus on behavior, risk, verification evidence, and deterministic comment resolution.

## Execution mode

This workflow has two modes:

- Draft mode (default): package the PR, collect evidence, and report risks.
- AI-review-loop mode: additionally run reviewer polling/comment-resolution loops and require merge-ready checks.

Use AI-review-loop mode only when the repository workflow and tooling support it.

## Workflow

1. Inspect branch status and changed files.
2. Group changes by behavior impact, not by commit order.
3. Identify user-visible changes, internal refactors, and risk areas.
4. Collect test evidence and list uncovered risk gaps.
5. Produce PR title, PR body, and reviewer checklist.
6. Open or update PR.
7. Run an immediate full review pass on the newly opened/updated PR head. Build unresolved actionable queue and address or rebut every item.
8. In AI-review-loop mode:
  - Run review loop until merge readiness criteria are met.
  - After each push in the review loop, explicitly trigger Copilot re-review for the new PR head.
  - Never post `@copilot review`; use reviewer-request APIs only to avoid triggering coding-agent PR creation.
  - Do not stop at "PR opened"; continue polling and resolving reviews/checks unless the user explicitly asks to stop.
9. In non-loop draft mode:
  - If the request is only PR packaging, you may return with findings and mitigation plan from the immediate review.
  - If the request includes merge readiness, continue until the pre-merge full review also runs on the final head and all actionable findings are resolved or explicitly rebutted.

## Completion Gate (Required)

A PR task is not complete until one of these is true:

1. Immediate post-open review requirement (all modes):
   - At least one full review pass executed after PR creation/open.
   - Any actionable item from that pass is classified as fix/reject and addressed before merge.

2. In AI-review-loop mode:
   - Merge-ready state reached:
   - No pending required checks.
   - No unresolved actionable review comments (PR review comments, PR review summaries, or issue comments).
   - AI-reviewer quiet window reached (default 10 minutes with no new actionable comments).
3. In draft mode:
   - PR package and verification evidence are complete.
   - No process blockers remain unresolved.
4. Before merge in all modes:
   - Run a second full review pass on current head.
   - Ensure actionable findings are resolved or explicitly rebutted.
   - Then report merge block clear.
5. User explicitly asks to stop before merge-ready.

If merge-ready is not reached in the current turn, report exact blocking state and continue the loop on next request.

### Pre-Final Verification (Mode-aware)

In draft mode: summarize verification evidence and any known gaps clearly; AI-loop-only metrics are not required.

In AI-review-loop mode: before concluding the PR task as complete, run a verification check against the live PR state. If any required metric is missing, stale, or cannot be computed from the current head commit, the task is **not complete**.

Required metrics:
- PR URL
- Unresolved actionable comment count
- Pending required checks count
- Last AI-reviewer comment timestamp (used to verify the AI-reviewer quiet window has elapsed; `null` when no AI-reviewer comments exist on current head)
- Copilot re-requested on current head (`true` only when Copilot re-review was explicitly requested after the latest push for `headRefOid`)
- Merge-ready boolean (`true` only when all gate conditions are satisfied)

Rules:
- Do not claim completion if any required metric is omitted.
- Do not claim completion using cached values after a new push/review event; repoll first.
- Treat missing or ambiguous review/check data as blocking and continue the loop.

### Required Metric Derivation (Deterministic)

1. `unresolved_actionable_comments`
- Count actionable PR review comments from threads where `isResolved == false` and `isOutdated == false`.
- Count actionable PR issue comments as unresolved when there is no later maintainer response that either:
  - references the comment URL/ID and links the fixing commit SHA, or
  - references the comment URL/ID and provides an explicit technical rebuttal.
- Use this combined unresolved queue size as the metric value.

2. `pending_required_checks`
- Prefer branch-protection required checks for the base branch.
- Count only required checks in queued/in-progress states as pending.
- Do not count optional checks as pending.
- Treat `neutral`, `skipped`, and `cancelled` required checks as not pending.
- If branch-protection required-check metadata is unavailable, fall back to `statusCheckRollup`/mergeability signals and treat ambiguous required-check state as pending (blocking).

3. `last_ai_reviewer_comment_timestamp`
- Use the latest timestamp from configured AI reviewers for signals attributable to current head:
  - PR review comments and PR review summaries tied to `headRefOid`.
  - PR issue comments with `created_at` greater than or equal to the push timestamp that produced current `headRefOid` (issue comments are not commit-scoped by GitHub).
- If no such comments exist for current head, set value to `null` (this counts as present, not omitted) and treat the AI quiet-window condition as satisfied for this metric.

4. `copilot_rerequested_on_head`
- Set `true` only when a Copilot reviewer-request action is recorded for the current `headRefOid` (for example, reviewer-request API call accepted on current head).
- Set `false` if no explicit trigger can be proven for current head.

### Freshness Rules (Repoll Required)

Treat verification metrics as fresh only when all are true for the last poll:
- Polled `headRefOid` matches current PR head commit.
- Latest review/comment timestamps (`reviews`, `review comments`, `issue comments`) are less than or equal to the last poll timestamp.
- Latest required-check status timestamps are less than or equal to the last poll timestamp.

If any push/review/comment/check update appears after last poll (via changed `headRefOid` or newer timestamps), repoll before deciding completion.

### Verification Block Format (Required)

Emit the verification block as a fenced `json` object with these exact keys:

```json
{
  "pr_url": "https://github.com/<owner>/<repo>/pull/<n>",
  "unresolved_actionable_comments": 0,
  "pending_required_checks": 0,
  "last_ai_reviewer_comment_timestamp": "2026-03-04T09:32:57Z",
  "copilot_rerequested_on_head": true,
  "merge_ready": true
}
```

Rules:
- If a value cannot be computed, use `null` and treat the PR as not merge-ready.
- Nullable key contract: `last_ai_reviewer_comment_timestamp` may be `null` or an ISO-8601 string.
- Do not rename keys. For non-null values, preserve the documented scalar types exactly.

### Last AI Reviewer Timestamp (Canonical)

`last_ai_reviewer_comment_timestamp` is the latest timestamp from configured AI-authored review signals attributable to the current head:
- PR review comments / summaries associated with `headRefOid`
- PR issue comments whose `created_at` is greater than or equal to the push timestamp for `headRefOid`

Author match uses case-insensitive exact-login regex `^(copilot-pull-request-reviewer|coderabbitai(\\[bot\\])?|sourcery-ai(\\[bot\\])?)$` by default, or a stricter configured override if the repo defines one.

### API Failure Handling (Fail Closed)

When polling GitHub state for verification metrics:
1. Retry up to 3 times with backoff (`2s`, `5s`, `10s`) on transient API failures (network, 5xx, rate-limit retry windows).
2. If any required metric is still unavailable after retries, set unknown metrics to `null`, set `merge_ready` to `false`, report the exact blocking API failure, and continue the loop.

## Review Parameters

1. Poll interval: `POLL_SECONDS` (default `300`).
2. Quiet window interval count: `QUIET_POLL_INTERVALS` (default `2`).
3. Quiet window duration is `POLL_SECONDS * QUIET_POLL_INTERVALS`.

## Comment Classification (Canonical)

1. Actionable comments:
  - Inline review comments, concrete issue comments, and PR review summaries that request code or documentation changes.
2. Non-actionable comments:
  - "review in progress", rate-limit notices, summaries/walkthroughs, ads/tips, and informational status updates.
3. If a status-style comment includes a concrete requested change, treat it as actionable.

## Run Completion Gate

Do not end the skill run while either condition is true:

1. Any review/check is still in progress (for example, CodeRabbit pending, Sourcery in progress, required checks pending).
2. Any actionable comment remains unresolved.

Only end the run when all are true:

1. All review and check statuses have completed.
2. All actionable comments have been resolved.
3. No new actionable comments have appeared for at least `QUIET_POLL_INTERVALS` poll intervals.
4. Copilot has been re-requested for the current head commit (when available in the repo) and no unresolved Copilot feedback remains.

Use the `Round Completion Heuristic` (from `references/review-loop-commands.md`) to determine when one review round has ended. Then apply this `Run Completion Gate` to decide whether to start another round or end the full skill run.

## Review Loop Protocol

1. Poll PR state for new reviews/comments and check results every `POLL_SECONDS` seconds.
2. Detect end of current round with this heuristic:
  - All checks are complete (no pending required checks).
  - No new actionable comments (from any reviewer, human or AI) for at least `QUIET_POLL_INTERVALS` poll intervals.
  - At least one signal from expected reviewers (required human reviewers and/or configured AI reviewers) on current head commit when possible.
3. Build unresolved comment queue from review comments, issue comments, and PR review summaries.
4. Classify comments using `Comment Classification (Canonical)` before acting.
5. For each unresolved actionable comment:
- Add a thumbs-up reaction first.
- Then either fix in code or reply with a technical rebuttal.
6. Never use "push to later feature" as the reason to skip a valid fix.
7. If rejecting a comment, provide specific evidence: incorrect assumption, constraint conflict, duplicate, or already addressed.
8. Push updates, post round summary, and request re-review.
9. Explicitly trigger Copilot re-review on each pushed head using command patterns in `references/review-loop-commands.md`.
10. Do not trigger Copilot using PR/issue comment mentions.
11. Verify whether Copilot produced a review signal for current `headRefOid`; if not, keep polling through the quiet window and report the exact state.
12. Sleep `POLL_SECONDS` seconds and re-poll.
13. Repeat until run completion gate is satisfied.

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
11. Verification block (required metrics listed in Pre-Final Verification)

The verification block must be a fenced `json` block and must follow `Verification Block Format (Required)`.

Example output section:

Section header: `## Verification`

```json
{
  "pr_url": "https://github.com/<owner>/<repo>/pull/<n>",
  "unresolved_actionable_comments": 0,
  "pending_required_checks": 0,
  "last_ai_reviewer_comment_timestamp": "2026-03-04T09:32:57Z",
  "copilot_rerequested_on_head": true,
  "merge_ready": true
}
```

`Merge readiness status` and the `Verification block` must agree. If they disagree, treat as not merge-ready and continue the loop.

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
