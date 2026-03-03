# Codex Operating Guide for This Project

## Intent
Use Codex the way you used Claude Code CLI, but with explicit separation between:
1. Project operating rules (`AGENTS.md`)
2. Reusable workflows (`skills/`)
3. Recurring execution (`automations`)

## Claude-to-Codex Mapping
1. `claude.md` -> `AGENTS.md`
- Project rules and behavior expectations for the coding agent.
2. Claude skills -> Codex skills
- Folder per skill with required `SKILL.md`.
3. Repeated manual prompts -> Codex automations
- Scheduled runs that create inbox items automatically.

## Project Conventions
1. Keep project-specific skills in `skills/`.
2. Keep planning and standards in `docs/`.
3. Keep `AGENTS.md` as the entry point for project rules and available skills.

## Skill Structure
Each skill should look like this:

```text
skills/<skill-name>/
  SKILL.md
  agents/openai.yaml
  references/
```

## How to Trigger Skills
1. Explicit trigger:
- Mention the skill name directly, for example `$github-pr`.
2. Implicit trigger:
- Ask for work that clearly matches the skill description.
3. Multi-skill trigger:
- Name multiple skills when needed, then request the final output format.

## Current Project Skills
1. `github-issue-refiner`
- Turns rough issues into implementation-ready specs.
2. `github-pr`
- Produces PR title/body, risk analysis, and test evidence package.
3. `ux-mockup-explorer`
- Produces raw HTML UX option packs and decision artifacts for UX-heavy issues.
4. `flutter-ux-theming`
- Applies project UX/theming standards for Flutter UI tasks.
5. `backend-ddd-vertical-slice`
- Applies project backend/API standards for pragmatic DDD + vertical slices.

## Practical Prompt Patterns
1. Refine issue:
- `Use $github-issue-refiner on this issue text and output final issue markdown.`
2. Prepare PR:
- `Use $github-pr for my current branch and draft a PR body with risk and test evidence.`
3. Explore UX options before implementation:
- `Use $ux-mockup-explorer for this issue and create option-a to option-e HTML mockups with a decision.md.`
4. Apply Flutter theming standards:
- `Use $flutter-ux-theming to build this screen and keep tokens + component themes consistent with project standards.`
5. Apply backend vertical-slice standards:
- `Use $backend-ddd-vertical-slice to implement this API feature with domain invariants and thin handlers.`
6. Scaffold from backend template:
- `Use $backend-ddd-vertical-slice and scaffold this feature from backend/templates/vertical-slice-template.`
7. Split large work:
- `Use $github-issue-refiner and split this into parent issue plus child issues by vertical slice.`

## Automation Strategy
Use automations for repeated checks, not one-off tasks.

Good first automations for this project:
1. Weekly backlog hygiene:
- Refine newly created rough issues into executable specs.
2. Daily release readiness:
- Scan current branch status and list release blockers.
3. Weekly KPI summary:
- Summarize conversion and retention metrics from latest data source.

## Suggested Next Skills
1. `github-issue-breakdown`
- Converts one large issue into linked child issues with sequencing.
2. `release-readiness`
- Runs release checklist and outputs go/no-go recommendation.
3. `incident-postmortem`
- Produces structured postmortem from logs and timeline notes.

## Operating Loop
1. Update `AGENTS.md` when project rules change.
2. Add or refine skills after repeated prompt patterns appear.
3. Add automation once a workflow repeats at least twice per week.
4. Keep docs and skills versioned in git with the product code.
