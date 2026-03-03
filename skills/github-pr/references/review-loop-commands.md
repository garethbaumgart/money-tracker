# Review Loop Commands

Use these command patterns when running PR review rounds.

## Variables

```bash
OWNER_REPO="<owner>/<repo>" # e.g. garethbaumgart/money-tracker
PR_NUMBER="<pr-number>"
AI_REVIEWERS_REGEX="copilot|coderabbit|sourcery"
POLL_SECONDS=300
```

## Poll PR State

```bash
gh pr view "$PR_NUMBER" \
  --repo "$OWNER_REPO" \
  --json url,headRefOid,reviewDecision,isDraft,statusCheckRollup,reviews
```

## Fetch Review Comments

```bash
gh api --paginate "repos/$OWNER_REPO/pulls/$PR_NUMBER/comments?per_page=100"
```

## Fetch Issue-Level Comments

```bash
gh api --paginate "repos/$OWNER_REPO/issues/$PR_NUMBER/comments?per_page=100"
```

## Identify AI Reviewer Comments

Review comments:

```bash
gh api --paginate "repos/$OWNER_REPO/pulls/$PR_NUMBER/comments?per_page=100" \
  --jq ".[] | select(.user.login | test(\"$AI_REVIEWERS_REGEX\"; \"i\")) | {id, user: .user.login, url: .html_url, body}"
```

Issue comments:

```bash
gh api --paginate "repos/$OWNER_REPO/issues/$PR_NUMBER/comments?per_page=100" \
  --jq ".[] | select(.user.login | test(\"$AI_REVIEWERS_REGEX\"; \"i\")) | {id, user: .user.login, url: .html_url, body}"
```

## Add Thumbs-Up Reaction Before Responding

PR review comment reaction:

```bash
COMMENT_ID="<pull-comment-id>"
gh api --method POST "repos/$OWNER_REPO/pulls/comments/$COMMENT_ID/reactions" \
  -H "Accept: application/vnd.github+json" \
  -f content='+1'
```

Issue comment reaction:

```bash
COMMENT_ID="<issue-comment-id>"
gh api --method POST "repos/$OWNER_REPO/issues/comments/$COMMENT_ID/reactions" \
  -H "Accept: application/vnd.github+json" \
  -f content='+1'
```

If a reaction already exists, ignore duplicate-reaction errors and continue.

## Resolve a Comment

1. Add thumbs-up.
2. Choose one path:
- `fix-now`: implement code change and reference commit in reply.
- `rebut`: explain technical reason with concrete evidence.
3. Do not use "later feature" as rationale to skip a valid fix.

## Round Completion Heuristic

Treat a review round as complete when all are true:

1. No pending required checks.
2. No new actionable AI reviewer comments for at least 2 poll intervals.
3. No unresolved actionable comments remain in queue.

Then push next iteration summary or mark PR merge-ready.

## Non-Actionable Comment Patterns

Do not block on comments matching these patterns unless they include a concrete requested change:

1. Review-in-progress status messages.
2. Rate-limit notices.
3. Auto-generated summaries/walkthroughs.
4. Marketing/tips/help footers.

## Polling Loop Skeleton

```bash
while true; do
  # 1) Poll PR/check status
  gh pr view "$PR_NUMBER" --repo "$OWNER_REPO" \
    --json mergeStateStatus,statusCheckRollup,headRefOid

  # 2) Fetch and classify comments
  gh api --paginate "repos/$OWNER_REPO/pulls/$PR_NUMBER/comments?per_page=100"
  gh api --paginate "repos/$OWNER_REPO/issues/$PR_NUMBER/comments?per_page=100"

  # 3) If actionable comments exist: react + fix/rebut + push + reply
  # 4) If no pending checks and no actionable comments for this interval: break

  sleep "$POLL_SECONDS"
done
```
