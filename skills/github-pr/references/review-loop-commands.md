# Review Loop Commands

Use these command patterns when running PR review rounds.

## Variables

```bash
OWNER_REPO="garethbaumgart/money-tracker"
PR_NUMBER="<pr-number>"
AI_REVIEWERS_REGEX="copilot|coderabbit|sourcery"
```

## Poll PR State

```bash
gh pr view "$PR_NUMBER" \
  --repo "$OWNER_REPO" \
  --json url,headRefOid,reviewDecision,isDraft,statusCheckRollup,reviews
```

## Fetch Review Comments

```bash
gh api "repos/$OWNER_REPO/pulls/$PR_NUMBER/comments?per_page=100"
```

## Fetch Issue-Level Comments

```bash
gh api "repos/$OWNER_REPO/issues/$PR_NUMBER/comments?per_page=100"
```

## Identify AI Reviewer Comments

Review comments:

```bash
gh api "repos/$OWNER_REPO/pulls/$PR_NUMBER/comments?per_page=100" \
  --jq ".[] | select(.user.login | test(\"$AI_REVIEWERS_REGEX\"; \"i\")) | {id, user: .user.login, url: .html_url, body}"
```

Issue comments:

```bash
gh api "repos/$OWNER_REPO/issues/$PR_NUMBER/comments?per_page=100" \
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
2. No new AI reviewer comments for 10 minutes.
3. No unresolved actionable comments remain in queue.

Then push next iteration summary or mark PR merge-ready.
