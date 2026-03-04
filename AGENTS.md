## Skills
A skill is a set of local instructions stored in a `SKILL.md` file.

### Available skills
- `github-issue-refiner`: Refine rough GitHub issues into decision-complete implementation specs with scope, acceptance criteria, and test plan. (file: `/Users/garethbaumgart/Source/money-tracker/skills/github-issue-refiner/SKILL.md`)
- `github-pr`: Prepare high-signal PR packages from local changes, including summary, risk analysis, and test evidence. (file: `/Users/garethbaumgart/Source/money-tracker/skills/github-pr/SKILL.md`)
- `ux-mockup-explorer`: Generate raw HTML UX option packs (A-E) and decision artifacts for UX-heavy issues before Flutter implementation. (file: `/Users/garethbaumgart/Source/money-tracker/skills/ux-mockup-explorer/SKILL.md`)
- `flutter-ux-theming`: Apply project Flutter UX/theming standards using Material 3, semantic tokens, ThemeExtension, and component themes. Use for UI feature build/refactor/review tasks. (file: `/Users/garethbaumgart/Source/money-tracker/skills/flutter-ux-theming/SKILL.md`)
- `backend-ddd-vertical-slice`: Implement backend/API features using pragmatic DDD + vertical slices with clear domain/application/infrastructure/presentation boundaries. (file: `/Users/garethbaumgart/Source/money-tracker/skills/backend-ddd-vertical-slice/SKILL.md`)
- `skill-creator`: Guide for creating or updating Codex skills. Use when creating new project skills. (file: `/Users/garethbaumgart/.codex/skills/.system/skill-creator/SKILL.md`)
- `skill-installer`: Install Codex skills from curated or GitHub sources into Codex home. (file: `/Users/garethbaumgart/.codex/skills/.system/skill-installer/SKILL.md`)

### How to use skills
- Trigger by naming a skill directly (for example, `$github-pr`) or by asking for work that clearly matches the skill description.
- For UX-heavy issue refinement, use `github-issue-refiner` plus `ux-mockup-explorer` and require selected option evidence before implementation.
- For any Flutter UI or theming task, default to `flutter-ux-theming` unless the user explicitly asks for a different approach.
- For any backend/API implementation or refactor task, default to `backend-ddd-vertical-slice` unless the user explicitly asks for a different approach.
- If multiple skills apply, use the minimal set and apply them in sequence.
- For implementation + PR tasks, prefer explicit completion prompts such as: `Implement issue #<n>, open PR, then continue review rounds until required checks pass and actionable comments are resolved (or I say stop).`
- Read only the needed sections/files from each skill to keep context lean.
- Prefer bundled `scripts/` and `references/` inside skills over rewriting the same workflow repeatedly.
- If a skill path is missing or unreadable, continue with a direct fallback workflow and note the gap.
