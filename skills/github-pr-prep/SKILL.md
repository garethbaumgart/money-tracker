---
name: github-pr-prep
description: Prepare pull requests from local changes with a clear summary, behavior changes, risk analysis, test evidence, and rollout notes. Use when a user asks to prepare a PR, write a PR description, assemble release notes, or perform pre-review quality checks before opening a PR.
---

# Github Pr Prep

## Overview

Generate a high-signal PR package that reviewers can process quickly.  
Focus on behavior, risk, verification evidence, and change clarity.

## Workflow

1. Inspect branch status and changed files.
2. Group changes by behavior impact, not by commit order.
3. Identify user-visible changes, internal refactors, and risk areas.
4. Collect test evidence and list uncovered risk gaps.
5. Produce PR title, PR body, and reviewer checklist.

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

## Quality Rules

1. Do not hide risk behind generic phrasing.
2. Separate confirmed test evidence from untested areas.
3. Link claims to changed files and observed behavior.
4. Keep summary concise; keep risk and testing explicit.
5. Prefer concrete reviewer instructions over broad requests.

## Required Pre-PR Checks

1. Branch is up to date with target branch.
2. CI checks run locally or in pipeline where possible.
3. Security-sensitive changes are explicitly called out.
4. API contract changes include OpenAPI updates.
5. Migration risk is documented for DB or billing changes.

## Reference Files

1. PR template and checklist:
- `references/pr-template.md`

Use that template when drafting final PR output.
