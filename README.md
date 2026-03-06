# Money Tracker

Phase 1 foundation repo for the Money Tracker mobile app and backend API.

## Prerequisites

- Flutter 3.32+ with Dart 3.8+
- .NET SDK 10.0+ (`global.json` pins `10.0.101`)

## First-Time Setup

From repo root:

```bash
git clone https://github.com/garethbaumgart/money-tracker.git
cd money-tracker
```

Verify toolchains:

```bash
flutter --version
dotnet --version
```

## OpenAPI Contract Artifact

When API endpoints or request/response models change:

```bash
./backend/scripts/export-openapi.sh
```

The command regenerates:

- `backend/openapi/openapi.v1.json`

CI regenerates this artifact and fails if the committed file has drift.

## Project Layout

```text
mobile/   Flutter app shell
backend/  .NET API shell and templates
docs/     Architecture and execution guides
skills/   Codex workflow skills
```

## New Developer Workflow

1. Read `docs/dev-guide/index.md`.
2. Choose your lane and read the lane guide:
   - Backend lane: `docs/dev-guide/backend.md`
   - Mobile lane: `docs/dev-guide/mobile.md`
   - Platform lane: `docs/dev-guide/platform.md`
3. Declare your lane before implementation and stay in that lane unless cross-lane work is explicitly approved.
4. Use skill routing for task type:
   - Pull request creation -> `$github-pr`
   - Backend feature implementation -> `$backend-ddd-vertical-slice`
   - Flutter UI/theming work -> `$flutter-ux-theming`
   - UX exploration -> `$ux-mockup-explorer`
   - Issue clarification/spec drafting -> `$github-issue-refiner`
5. Implement the minimum correct change (avoid drive-by refactors).
6. Verify before opening a PR using `docs/dev-guide/verification.md`.

## What Is a Lane?

A lane is the area of the codebase you are responsible for in a task.

- Backend lane: `backend/**` (API/domain/application/infrastructure changes)
- Mobile lane: `mobile/**` (Flutter UI/app logic changes)
- Platform lane: root tooling/docs, and (if present) `.github/**` and `scripts/**`

Why this exists:

- Reduces accidental cross-area changes
- Makes parallel work safer
- Keeps reviews focused and faster

If a task needs changes in multiple lanes, split it into separate tasks or explicitly approve cross-lane work first.

## Agent-Driven Workflow

Use this loop when you want agents to help from issue creation through merge.
Keep this section as a quick-start summary. Detailed workflow rules live in:

- `docs/dev-guide/index.md`
- `docs/dev-guide/verification.md`
- `AGENTS.md`

1. Capture a rough issue.
2. Refine it with `$github-issue-refiner` so scope, acceptance criteria, and tests are explicit.
3. Choose lane ownership (backend/mobile/platform).
4. Implement with the lane-appropriate skill.
5. Verify with tests/evidence (see `docs/dev-guide/verification.md`).
6. Open/update PR using `$github-pr` and run review loops until merge-ready.

### How Skills Are Invoked

In Codex, invoke a skill by naming it in your prompt (for example, `$github-pr` or `$backend-ddd-vertical-slice`).

- Skill routing rules: `AGENTS.md`
- Skill definitions: `skills/*/SKILL.md`

### Parallel Worker Pattern

Use parallel workers only when changes are independent.

Do not parallelize when work is tiny, tightly coupled, or needs strict sequencing.

1. Split by lane or isolated module.
2. Give each worker a clear acceptance checklist.
3. Require each worker to provide:
   - Files changed
   - Verification commands run
   - Risks/assumptions
4. Integrate, run final verification, then open one PR (or stacked PRs if needed).

## Mobile Shell

From repo root:

```bash
cd mobile
flutter pub get
flutter analyze
flutter test
flutter run
```

## Backend API Shell

From repo root:

```bash
dotnet restore backend/MoneyTracker.slnx
dotnet build backend/MoneyTracker.slnx
dotnet run --project backend/src/MoneyTracker.Api/MoneyTracker.Api.csproj
```

## Pull Request Workflow

Before opening a PR:

1. Ensure tests pass.
2. Ensure acceptance criteria are satisfied.
3. Use `$github-pr` to generate the PR description.
4. Include verification evidence and reference the issue being resolved.

## CI Quality Gates

Issue #7 adds CI parity scripts and required status checks for `main`.

Local parity commands:

```bash
./scripts/ci/backend-quality.sh
./scripts/ci/mobile-quality.sh
./scripts/ci/security-baseline.sh
```

Security baseline script requires `gitleaks`, `osv-scanner`, and `jq`.

Branch protection setup and required check names:

- `docs/ci-required-checks.md`

## First Contribution Path

1. Pick an issue with clear acceptance criteria.
2. Choose your lane and follow the lane guide in `docs/dev-guide/`.
3. Implement the change and run relevant checks:
   - Mobile: `flutter analyze && flutter test`
   - Backend: `dotnet build backend/MoneyTracker.slnx && dotnet test backend/MoneyTracker.slnx`
4. Open a PR using the workflow above.

## Notes

- Backend solution uses `.slnx` (default format for current .NET SDK).
- Canonical workflow + lane docs:
  - `docs/dev-guide/index.md`
  - `docs/dev-guide/backend.md`
  - `docs/dev-guide/mobile.md`
  - `docs/dev-guide/platform.md`
  - `docs/dev-guide/verification.md`
- Feature implementation standards:
  - `docs/App-Build-GuideRails.md`
  - `docs/Backend-DDD-Vertical-Slice-Standards.md`
  - `docs/Flutter-UX-Theming-Standards.md`
