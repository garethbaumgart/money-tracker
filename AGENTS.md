## Skills
A skill is a set of local instructions stored in a `SKILL.md` file.

## Skill Routing

Workflow and skill routing are centralized in:

- [workflow-catalog](docs/dev-guide/workflow-catalog.md)
- [agent workflow quick start](docs/dev-guide/agent-workflow-quick-start.md)

## Worker Startup Rules

When beginning a task, workers must first read:

`docs/dev-guide/index.md`

Then read the guide corresponding to their lane:

Backend work → `docs/dev-guide/backend.md`  
Mobile work → `docs/dev-guide/mobile.md`  
Platform work → `docs/dev-guide/platform.md`

Workers must declare their lane before implementation begins.

Use [workflow catalog](docs/dev-guide/workflow-catalog.md) as the canonical source for startup format, lane declaration schema, skill routing, and PR mode policy.

Before implementation, emit a compact task declaration in the session:

- `Lane:` backend | mobile | platform
- `Task type:` idea-intake | issue-refinement | implementation | ux-design | parallel-issues | pr-only
- `Primary skills:` one or more `$skill-name`
- `Merge-ready mode:` draft | ai-review-loop

If `merge-ready mode` is `ai-review-loop`, it must be supported in the repo/environment before running that flow.

## Pull Request Workflow

Before opening a pull request (draft mode):

1. Ensure tests pass
2. Ensure acceptance criteria are satisfied
3. Use the `$github-pr` skill to generate the PR description
4. Open the PR
5. Immediately run a full PR review pass on the created PR before considering implementation done.

Before claiming merge-ready completion:

1. If the post-open review produced actionable findings or the issue is high risk, run a second full review pass on the latest PR head before merge.
2. If the post-open review had no actionable findings and risk is low, run a pre-merge checklist recheck on current PR head.
3. Ensure required checks and PR comments are cleared per repo policy.
4. Address all actionable findings (or provide explicit technical rebuttals) before merge.
5. If `ai-review-loop` is declared, use the full `$github-pr` completion contract (including AI reviewer quiet-window checks).
6. If AI review loop is not available in the environment, provide explicit evidence of review closure and the PR's required verification only.

PRs must include verification evidence and reference the issue they resolve.

PR mode defaults are defined in [docs/dev-guide/workflow-catalog.md](docs/dev-guide/workflow-catalog.md).

### Skills references

- Use [workflow-catalog skill matrix](docs/dev-guide/workflow-catalog.md) for routing.
- If multiple skills apply, run the minimal set in sequence; read only needed skill sections; fall back to direct workflow only if a skill is unavailable.
