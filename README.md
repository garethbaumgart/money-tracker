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
- Platform lane: `.github/**`, `scripts/**`, and root tooling/docs

Why this exists:

- Reduces accidental cross-area changes
- Makes parallel work safer
- Keeps reviews focused and faster

If a task needs changes in multiple lanes, split it into separate tasks or explicitly approve cross-lane work first.

## Agent-Driven Workflow

Use this loop when you want agents to help from issue creation through merge.

1. Create or capture the rough issue.
2. Refine it with `$github-issue-refiner` so scope, acceptance criteria, and test plan are explicit.
3. Assign lane(s) and split work into parallel worker tasks when possible (one lane per worker).
4. Execute implementation with lane-appropriate skill:
   - Backend: `$backend-ddd-vertical-slice`
   - Mobile UI/theming: `$flutter-ux-theming`
   - UX-heavy design exploration: `$ux-mockup-explorer` before Flutter implementation
5. Verify each worker output with tests and evidence.
6. Open PR using `$github-pr`, then run review rounds until merge-ready.

### Parallel Worker Pattern

Use parallel workers only when changes are independent.

1. Split by lane or isolated module.
2. Give each worker a clear acceptance checklist.
3. Require each worker to provide:
   - Files changed
   - Verification commands run
   - Risks/assumptions
4. Integrate, run final verification, then open one PR (or stacked PRs if needed).

### Example Prompts

Issue refinement:

```text
Use $github-issue-refiner on issue #<n>. Produce a decision-complete spec with scope, acceptance criteria, and test plan.
```

Parallel execution:

```text
Implement issue #<n> with parallel workers:
- Worker 1 (backend lane) uses $backend-ddd-vertical-slice
- Worker 2 (mobile lane) uses $flutter-ux-theming
Each worker must stay in-lane and return verification evidence.
```

PR and review loop:

```text
Use $github-pr for this branch. Open/update the PR and continue review rounds until all actionable comments are resolved and required checks are complete.
```

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
