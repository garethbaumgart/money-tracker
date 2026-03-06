# Verification Guide

Every pull request must include verification evidence.

Verification should include:

- Test results (commands + outcomes)
- Evidence of changed behavior
- Screenshots or logs where applicable

## PR Requirements

PR descriptions must include:

- Summary  
- Changes made  
- Acceptance criteria checklist  
- Verification evidence  
- Risk notes
- Full review log for the initial post-open review and pre-merge review

## Canonical verification contract

Default mode (no configured AI reviewer loop):

- Verification evidence is required for all changed acceptance criteria.
- Use a concise evidence table and note any known gaps.
- A full PR review pass is required immediately after PR creation and again before merge.

AI-review-loop mode (explicitly enabled for the repo/workspace):

- Run the `$github-pr` loop contract from `skills/github-pr/SKILL.md`, including AI reviewer quiet-window checks and merge-readiness metrics.

When a mode cannot be executed, explicitly document why and proceed in default mode.

Use this minimal evidence block (recommended):

```text
Verification
- PR URL:
- Required checks run:
- AC mapped to tests:
- Manual checks:
- Known gaps:
- Merge-readiness status:
```

## Skill Usage

- Use the `$github-pr` skill to generate PR descriptions and optional review-loop evidence.
- If AI review loop is required, run both the PR template and the `$github-pr` completion verification contract.
