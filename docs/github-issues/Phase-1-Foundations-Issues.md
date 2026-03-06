# Phase 1 GitHub Issue Breakdown (Foundations and Skeleton)

Use this file to create GitHub issues for Phase 1 execution.

## Parent Epic

### Title
`[Epic] Phase 1 - Foundations and Skeleton`

### 1. Problem Statement
- We have strategy and standards, but no executable app baseline.
- Without foundation work, feature delivery will be slow and inconsistent.

### 2. Goals and Non-Goals
- Goals:
  - Establish runnable mobile and backend skeletons.
  - Lock CI, OpenAPI, testing, and architecture defaults.
  - Enable fast slice-by-slice implementation from Phase 2 onward.
- Non-goals:
  - No full feature completion (bank sync, subscriptions, advanced UX).

### 3. Scope
- In scope:
  - Issues P1-1 through P1-8 in this document.
- Out of scope:
  - Production feature depth beyond baseline stubs.

### 4. Current Behavior
- Repo currently contains planning docs, skills, and templates only.
- No mobile app runtime or backend API runtime exists.

### 5. Proposed Behavior
- Complete all child issues and satisfy Phase 1 exit criteria from `docs/Execution-Phases.md`.

### 6. Technical Plan
- Touchpoints by path/module:
  - `mobile/`
  - `backend/`
  - `.github/workflows/` (or equivalent CI location)
  - `docs/`
- New components/interfaces:
  - App shells, API shell, baseline endpoints, OpenAPI contract pipeline.
- Migration or compatibility notes:
  - No migration required; greenfield baseline.

### 7. API/Schema Contract Changes
- Endpoints/types affected:
  - Baseline endpoint set from `docs/App-Build-GuideRails.md`.
- Backward compatibility strategy:
  - Not applicable for first release baseline.

### 8. Acceptance Criteria
- [ ] All Phase 1 child issues are completed.
- [ ] CI builds and tests both mobile and backend.
- [ ] OpenAPI artifact exists and is validated in CI.
- [ ] Vertical slice template has been exercised in real code once.

### 9. Test Plan
- Unit:
  - Baseline domain and handler tests.
- Integration:
  - API startup and test host baseline.
- E2E:
  - Basic smoke route and app startup checks.
- Non-functional:
  - Lint/analyze and contract checks in CI.

### 10. Rollout and Monitoring
- Feature flags:
  - Not required for foundation-only slice.
- Release steps:
  - Merge behind trunk-based short PRs.
- Metrics/alerts:
  - CI success rate and test stability only.

### 11. Risks and Mitigations
- Risk:
  - Over-engineering foundation work.
- Mitigation:
  - Keep each issue minimal and acceptance-criteria driven.

### 12. Dependencies
- Upstream:
  - None.
- External:
  - Toolchain availability (.NET 10, Flutter SDK, CI runtime).
- Sequencing notes:
  - P1-1 and P1-2 should start first.

---

## P1-1: Monorepo Bootstrap for Mobile and Backend

### Title
`[P1-1] Bootstrap mobile/ and backend/ project skeletons`

### Problem Statement
- There is no runnable app code layout yet.

### Goals and Non-Goals
- Goals:
  - Create initial `mobile/` and `backend/` roots with buildable scaffolds.
- Non-goals:
  - Full feature implementation.

### Scope
- In scope:
  - Directory layout aligned to guide rails.
  - Minimal build scripts and readme notes for local run.
- Out of scope:
  - Business feature logic.

### Current Behavior
- Repo only has docs/skills/templates.

### Proposed Behavior
- `mobile/` and `backend/` can be built locally with baseline targets.

### Technical Plan
- Touchpoints:
  - `mobile/`
  - `backend/`
- New components/interfaces:
  - App entry points and baseline solution/workspace files.

### API/Schema Contract Changes
- None.

### Acceptance Criteria
- [ ] `mobile/` exists with runnable app shell.
- [ ] `backend/` exists with runnable API shell.
- [ ] Build commands documented in repo.

### Test Plan
- Unit: none required for scaffold.
- Integration: startup command succeeds.
- E2E: not applicable.
- Non-functional: lint/analyze command runs.

### Rollout and Monitoring
- Release steps:
  - Merge once both entry points are stable.

### Risks and Mitigations
- Risk:
  - Tooling mismatch between local and CI.
- Mitigation:
  - Pin SDK/runtime versions early.

### Dependencies
- Upstream: none.
- Sequencing:
  - Start first.

---

## P1-2: Backend API Host and Module Registration Baseline

### Title
`[P1-2] Create backend API host with module registration and health endpoints`

### Problem Statement
- No backend runtime exists to host vertical slices.

### Goals and Non-Goals
- Goals:
  - Establish API host, DI registration, and health checks.
- Non-goals:
  - Production business endpoints.

### Scope
- In scope:
  - API startup, dependency registration, health route.
  - Standard error response shape.
- Out of scope:
  - Real feature workflows.

### Current Behavior
- No API host code exists.

### Proposed Behavior
- API starts and exposes minimal operational routes.

### Technical Plan
- Touchpoints:
  - `backend/src/`
- New components/interfaces:
  - Composition root and module bootstrap.

### API/Schema Contract Changes
- Endpoints/types affected:
  - Health endpoint + error envelope.

### Acceptance Criteria
- [ ] API starts with environment config.
- [ ] Health endpoint returns success.
- [ ] Standard error contract is defined.

### Test Plan
- Unit: basic error mapper tests.
- Integration: host startup and health endpoint test.
- E2E: smoke call in CI.
- Non-functional: structured logging enabled.

### Rollout and Monitoring
- Metrics/alerts:
  - Health route response and startup failures.

### Risks and Mitigations
- Risk:
  - Startup complexity too early.
- Mitigation:
  - Keep host lean and additive.

### Dependencies
- Depends on:
  - P1-1.

---

## P1-3: Flutter App Shell and Theme Foundation

### Title
`[P1-3] Build Flutter app shell with Material 3 theme and token foundation`

### Problem Statement
- Mobile app has no executable shell or standardized theming layer.

### Goals and Non-Goals
- Goals:
  - Create app shell, route shell, and theme/token baseline.
- Non-goals:
  - Final feature UI.

### Scope
- In scope:
  - `ThemeData`, `ColorScheme`, `ThemeExtension`, base navigation shell.
- Out of scope:
  - Advanced screens.

### Current Behavior
- No mobile code exists.

### Proposed Behavior
- Mobile app boots with light/dark/system theme support and starter navigation.

### Technical Plan
- Touchpoints:
  - `mobile/lib/app/`
  - `mobile/lib/shared_kernel/`

### API/Schema Contract Changes
- None.

### Acceptance Criteria
- [ ] App launches with themed home shell.
- [ ] Token extension exists for custom semantic tokens.
- [ ] Base component themes are centrally configured.

### Test Plan
- Unit: token extension behavior.
- Integration: app startup test.
- E2E: smoke boot in CI.
- Non-functional: accessibility sanity checks for text contrast.

### Rollout and Monitoring
- Feature flags:
  - Not needed.

### Risks and Mitigations
- Risk:
  - Over-customized component abstraction too early.
- Mitigation:
  - Compose Material first.

### Dependencies
- Depends on:
  - P1-1.

---

## P1-4: Baseline Vertical Slice Implementation (Households Create)

### Title
`[P1-4] Implement first real backend vertical slice for household creation`

### Problem Statement
- The architecture is defined but unproven in a real module.

### Goals and Non-Goals
- Goals:
  - Validate pragmatic DDD + vertical slice in production-like code.
- Non-goals:
  - Full household feature set.

### Scope
- In scope:
  - Domain entity/value object baseline.
  - Command + handler.
  - Endpoint and repository abstraction.
- Out of scope:
  - Invite flow and permissions depth.

### Current Behavior
- Only reusable template exists.

### Proposed Behavior
- One real slice exists under `backend/src/Modules/Households`.

### Technical Plan
- Touchpoints:
  - `backend/src/Modules/Households/Domain`
  - `backend/src/Modules/Households/Application`
  - `backend/src/Modules/Households/Infrastructure`
  - `backend/src/Modules/Households/Presentation`

### API/Schema Contract Changes
- Endpoints/types affected:
  - `POST /households` baseline contract.

### Acceptance Criteria
- [ ] Endpoint creates household record (or stubbed persistence path).
- [ ] Domain invariants are enforced in domain layer.
- [ ] Handler contains orchestration only.
- [ ] OpenAPI reflects endpoint contract.

### Test Plan
- Unit: domain invariants.
- Integration: handler + repository behavior.
- E2E: endpoint happy path + validation failure.
- Non-functional: error codes are machine-readable.

### Rollout and Monitoring
- Metrics/alerts:
  - Request success/error rates for household create.

### Risks and Mitigations
- Risk:
  - Business logic leaking into endpoint layer.
- Mitigation:
  - Enforce review checklist from backend standards.

### Dependencies
- Depends on:
  - P1-2.

---

## P1-5: OpenAPI Contract Pipeline and API Standards Enforcement

### Title
`[P1-5] Add OpenAPI generation and contract validation checks`

### Problem Statement
- API contracts can drift without automated checks.

### Goals and Non-Goals
- Goals:
  - Ensure endpoint changes always update OpenAPI artifacts.
- Non-goals:
  - Full client code generation.

### Scope
- In scope:
  - OpenAPI artifact generation.
  - CI check for contract drift.
- Out of scope:
  - SDK generation pipeline.

### Current Behavior
- No OpenAPI check exists.

### Proposed Behavior
- CI fails when API contract changes are not reflected.

### Technical Plan
- Touchpoints:
  - Backend build pipeline
  - CI workflow config

### API/Schema Contract Changes
- Endpoints/types affected:
  - Contract baseline for current endpoints.

### Acceptance Criteria
- [ ] OpenAPI spec is generated and committed.
- [ ] CI validates spec changes.
- [ ] PR guidance references contract checks.

### Test Plan
- Unit: none.
- Integration: generation command in CI.
- E2E: none.
- Non-functional: deterministic output in CI.

### Rollout and Monitoring
- Metrics/alerts:
  - CI failure visibility for contract drift.

### Risks and Mitigations
- Risk:
  - Non-deterministic spec output.
- Mitigation:
  - Pin generation tooling and normalize ordering.

### Dependencies
- Depends on:
  - P1-2.

---

## P1-6: CI Pipeline for Quality Gates

### Title
`[P1-6] Configure CI for build, lint/analyze, tests, and security checks`

### Problem Statement
- No automated gate exists to protect trunk quality.

### Goals and Non-Goals
- Goals:
  - Enforce baseline quality and prevent regressions.
- Non-goals:
  - Full deployment automation to production.

### Scope
- In scope:
  - Mobile and backend build checks.
  - Test execution.
  - Static analysis/lint checks.
- Out of scope:
  - Advanced release orchestration.

### Current Behavior
- No CI pipeline exists.

### Proposed Behavior
- Every PR must pass core quality gates.

### Technical Plan
- Touchpoints:
  - `.github/workflows/` (or CI equivalent)

### API/Schema Contract Changes
- None.

### Acceptance Criteria
- [ ] CI runs on PR and main.
- [ ] Build + tests + analyze checks are required.
- [ ] Security/dependency scan baseline added.

### Test Plan
- Integration: validate workflow runs on sample PR.
- Non-functional: CI runtime is stable and repeatable.

### Rollout and Monitoring
- Metrics/alerts:
  - CI pass rate and flaky test tracking.

### Risks and Mitigations
- Risk:
  - Slow pipeline reduces dev velocity.
- Mitigation:
  - Split quick checks vs extended checks.

### Dependencies
- Depends on:
  - P1-1, P1-2, P1-3.

---

## P1-7: Environment and Configuration Baseline

### Title
`[P1-7] Define environment config model for local, staging, and production`

### Problem Statement
- Environment handling is undefined and can cause drift or secrets leakage.

### Goals and Non-Goals
- Goals:
  - Standardize config loading and secret boundaries.
- Non-goals:
  - Full infrastructure-as-code rollout.

### Scope
- In scope:
  - Config schema, environment variable conventions, secret handling guide.
- Out of scope:
  - Complete deployment automation.

### Current Behavior
- No environment convention exists.

### Proposed Behavior
- Local/staging/prod configs are explicitly modeled and documented.

### Technical Plan
- Touchpoints:
  - Backend and mobile config entry points
  - `docs/`

### API/Schema Contract Changes
- None.

### Acceptance Criteria
- [ ] Config keys and required values are documented.
- [ ] App startup fails fast on missing required configuration.
- [ ] No secrets are committed.

### Test Plan
- Unit: config validation tests.
- Integration: startup in each environment profile.
- Non-functional: secret scanning in CI.

### Rollout and Monitoring
- Metrics/alerts:
  - Startup/configuration failure alerts.

### Risks and Mitigations
- Risk:
  - Misconfigured environments block progress.
- Mitigation:
  - Provide `.env.example` patterns and validation.

### Dependencies
- Depends on:
  - P1-1, P1-2, P1-3.

---

## P1-8: Observability Baseline (Logging, Tracing IDs, Error Reporting Hooks)

### Title
`[P1-8] Add baseline observability for backend and mobile startup paths`

### Problem Statement
- Without observability, failures in early phases are hard to diagnose.

### Goals and Non-Goals
- Goals:
  - Add structured logs and correlation IDs in critical paths.
- Non-goals:
  - Full SRE-grade monitoring suite.

### Scope
- In scope:
  - Structured logging conventions.
  - Correlation ID propagation for backend requests.
  - Error reporting hook stubs.
- Out of scope:
  - Full metrics warehouse integration.

### Current Behavior
- No standardized observability baseline.

### Proposed Behavior
- Core startup and request flows are traceable in logs.

### Technical Plan
- Touchpoints:
  - Backend request pipeline
  - Mobile app bootstrap error handling
  - Docs for logging conventions

### API/Schema Contract Changes
- None.

### Acceptance Criteria
- [ ] Backend logs include correlation/request IDs.
- [ ] Core exception paths are logged with stable shape.
- [ ] Mobile captures startup exceptions via pluggable reporter.

### Test Plan
- Unit: logger wrapper/formatter tests.
- Integration: request path includes correlation ID.
- Non-functional: log shape consistency checks.

### Rollout and Monitoring
- Metrics/alerts:
  - Error rate visibility for startup and baseline endpoints.

### Risks and Mitigations
- Risk:
  - Logging noise without structure.
- Mitigation:
  - Define event names and log schema.

### Dependencies
- Depends on:
  - P1-2, P1-3.

---

## Recommended Issue Execution Order
1. P1-1 Monorepo bootstrap
2. P1-2 Backend API host baseline
3. P1-3 Flutter app shell baseline
4. P1-6 CI pipeline
5. P1-7 Environment/config baseline
6. P1-4 First real vertical slice (households create)
7. P1-5 OpenAPI contract pipeline
8. P1-8 Observability baseline

## Labels to Use
1. `phase:1-foundations`
2. `type:epic` or `type:feature`
3. `lane:backend` / `lane:mobile` / `lane:platform` / `lane:cross-cutting`
4. `status:triage` / `status:refined` / `status:ready` / `status:in-progress` / `status:blocked` / `status:review` / `status:done`
