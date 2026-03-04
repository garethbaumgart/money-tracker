# Issue Refinement Template

## 1. Problem Statement
- What user or business problem is being solved?
- What evidence suggests this matters now?

## 2. Goals and Non-Goals
- Goals:
- Non-goals:

## 3. Scope
- In scope:
- Out of scope:

## 4. Current Behavior
- System behavior today:
- Known constraints:

## 5. Proposed Behavior
- Functional behavior:
- UX behavior:
- Data behavior:

## 6. Technical Plan
- Touchpoints by path/module:
- New components/interfaces:
- Migration or compatibility notes:

## 7. API/Schema Contract Changes
- Endpoints/types affected:
- Request/response changes:
- Backward compatibility strategy:

## 8. UX Exploration Requirement
- Required: Yes/No
- Trigger reason:
- Mockup pack path:
- Recommended option:
- Selected option:
- Implementation blocked until selected: Yes/No

## 9. Acceptance Criteria
- [ ] AC-1: <criterion>
- [ ] AC-2: <criterion>
- [ ] AC-3: <criterion>

## 10. Test Plan
- Required Test Matrix (group by type; list scenario-exact tests, not just broad categories):
  - Matrix rows are for automated tests only.
  - Fields required per test:
    - Test ID
    - Mapped AC (`AC-<n>`)
    - Scenario
    - Expected assertion
    - Suggested path/module
    - New or existing test
  - Unit:
  - Component:
  - Integration:
    - E2E:
    - Non-functional:
- AC coverage rule: every `AC-<n>` must appear in at least one matrix row or be listed in `Manual-only ACs`.
- Negative-path rule: add at least one negative-path automated test for each behavior-changing AC unless not applicable, with rationale.
- Manual-only ACs (if any): justify why automation is not feasible yet.
- Non-functional/exploratory AC handling:
  - If strict 1:1 automation or a negative-path test is not meaningful, mark N/A with rationale and define a measurable verification probe/check.

## 11. Rollout and Monitoring
- Feature flags:
- Release steps:
- Metrics/alerts:

## 12. Risks and Mitigations
- Risk:
- Mitigation:

## 13. Dependencies
- Upstream:
- External:
- Sequencing notes:

## 14. Open Questions (Optional)
- Include only unresolved, high-impact decisions that block or materially change implementation.
