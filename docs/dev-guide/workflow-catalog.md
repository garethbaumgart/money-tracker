# Workflow Catalog

## Quick Start (required)

- `Lane:` backend | mobile | platform
- `Task type:` implementation | issue-refinement | ux-design | parallel-issues | pr-only
- `Primary skills:` one or more `$skill-name`
- `Merge-ready mode:` draft | ai-review-loop

Before work:

1. Read [docs/dev-guide/index.md](docs/dev-guide/index.md)
2. Read the lane guide:
   - backend → `docs/dev-guide/backend.md`
   - mobile → `docs/dev-guide/mobile.md`
   - platform → `docs/dev-guide/platform.md`
3. For PR-facing work, read [docs/dev-guide/verification.md](docs/dev-guide/verification.md)

## Skill routing (single source of truth)

- PR creation → `$github-pr`
- Backend/API implementation → `$backend-ddd-vertical-slice`
- Flutter UI/theming implementation → `$flutter-ux-theming`
- UX exploration/options → `$ux-mockup-explorer` (+ optionally `$github-issue-refiner` first)
- Issue refinement/spec drafting → `$github-issue-refiner`
- Multi-issue execution → `$execute-issues-parallel`
- Skill creation/update → `$skill-creator` (system path)
- Skill installation → `$skill-installer` (system path)

## Lane guardrails

- backend: prefer `backend/**`
- mobile: prefer `mobile/**`
- platform: prefer `.github/**` and root tooling
- any cross-lane change should be explicitly approved

## PR mode matrix

- Draft mode (default): PR package + verification evidence required; AI-loop metrics are optional.
- AI-review-loop mode: merge-ready loop, comment resolution, and AI reviewer metrics required.

## Skills artifact table

- PR creation: `skills/github-pr/SKILL.md`
- Backend: `skills/backend-ddd-vertical-slice/SKILL.md`
- Flutter/theming: `skills/flutter-ux-theming/SKILL.md`
- UX options: `skills/ux-mockup-explorer/SKILL.md`
- Issue refinement: `skills/github-issue-refiner/SKILL.md`
- Multi-issue execution: `skills/execute-issues-parallel/SKILL.md`
- Skill operations: `~/.codex/skills/.system/skill-creator/SKILL.md`, `~/.codex/skills/.system/skill-installer/SKILL.md`
