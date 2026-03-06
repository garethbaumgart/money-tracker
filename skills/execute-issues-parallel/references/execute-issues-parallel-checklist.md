# Execute Issues Parallel Checklist

## 1. Pre-flight
- Confirm list of issue numbers from the user prompt.
- Run `gh issue view <n>` for each requested issue.
- Build dependency graph: mark each issue as `ready`, `in-set-blocked`, or `externally-blocked`.
- Report dependency summary and chosen execution option (wave / ready-only / custom order).

## 2. Worktree Setup (per issue)
- Fetch base refs: `git fetch origin`.
- Create worktree at `../<repo-name>-issue-<number>`.
- Create branch `codex/issue-<number>-<slug>` from `origin/main` (or user-specified base).
- If worktree or branch already exists, append `-v2`, `-v3`, and so on.

## 3. Lane and Skill Routing (per issue)
- Declare lane: Backend, Mobile, or Platform.
- Read `docs/dev-guide/index.md` and the relevant lane guide.
- Route to the appropriate implementation skill:
  - Backend/API → `$backend-ddd-vertical-slice`
  - Flutter UI/theming → `$flutter-ux-theming`
  - UX design options needed → `$ux-mockup-explorer` first
  - Ambiguous spec → `$github-issue-refiner` first

## 4. Implementation (per issue)
- Implement in the issue worktree only; do not mix issue scopes.
- Follow the routed skill's checklist.
- Stay within declared lane unless the issue explicitly requires cross-lane edits.

## 5. Verification (per issue)
- Run tests/checks for changed areas only.
- Confirm all acceptance criteria are satisfied.
- Record test evidence (commands run, pass/fail output).

## 6. PR (per issue)
- Use `$github-pr` to generate PR title, body, and reviewer checklist.
- Reference the issue number in the PR (closes #n).
- Continue review rounds until merge-ready unless the user explicitly says stop.

## 7. Reporting
- Dependency summary and chosen option.
- Worktree path and branch per issue.
- Verification evidence per issue.
- PR URL per issue.
- Status per issue: `completed`, `in review`, `blocked`, or `failed`.
