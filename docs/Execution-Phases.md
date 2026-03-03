# Execution Phases (MVP to Monetization)

This roadmap turns the locked strategy into build phases that can be executed by a 1-2 builder team.

## Phase 1: Foundations and Skeleton (Weeks 1-2)
### Objective
Establish production-grade project foundations so feature work can proceed safely and quickly.

### In Scope
1. Repository scaffolding for `mobile/` and `backend/`.
2. Backend module skeleton and first vertical slice baseline.
3. Flutter app shell, routing shell, and theme/token foundation.
4. CI quality gates and test harness baseline.
5. OpenAPI baseline and API error contract conventions.
6. Environment setup for local, staging, production.

### Out of Scope
1. Full user onboarding UX.
2. Bank sync implementation.
3. Subscription purchase flow.

### Exit Criteria
1. Both apps build in CI.
2. Sample end-to-end request path exists (`POST /households` or equivalent stub).
3. OpenAPI artifact is generated and checked in CI.
4. Minimal smoke tests run in pipeline.
5. Team can create new slices from template without architecture decisions.

## Phase 2: Core Household and Budgeting MVP (Weeks 3-5)
### Objective
Ship core shared budgeting value without bank automation.

### In Scope
1. Auth, household creation, partner invite flow.
2. Manual transactions and category budgets.
3. Shared household dashboard basics.
4. Bill reminder baseline and notifications plumbing.

### Exit Criteria
1. Two users can share one household.
2. Household budget setup and manual tracking works end-to-end.
3. Core domain, application, and endpoint tests cover critical flows.

## Phase 3: Bank Connectivity and Sync Reliability (Weeks 5-7)
### Objective
Add bank integration through Basiq for AU/NZ with operational resilience.

### In Scope
1. Basiq link/session flow and callback handling.
2. Transaction sync jobs, dedupe, retry, idempotency.
3. Consent lifecycle and failure handling UX.
4. Pilot quality metrics for NZ fallback decision.

### Exit Criteria
1. Successful account linking and recurring sync on supported institutions.
2. Idempotent webhook/sync processing.
3. Observability and alerting on sync failures.

## Phase 4: Monetization and Entitlements (Weeks 7-8)
### Objective
Activate paid conversion with stable subscription mechanics.

### In Scope
1. RevenueCat integration and webhook processing.
2. Entitlement evaluation API and feature gating.
3. Trial handling, restore purchases, cancellation state handling.
4. Paywall integration in mobile with annual-first presentation.

### Exit Criteria
1. Trial-to-paid flow works across iOS and Android.
2. Entitlements update correctly with webhook events.
3. Premium gating is deterministic and test-covered.

## Phase 5: Insights, Retention, and Launch Hardening (Weeks 8-10)
### Objective
Improve paid value and launch readiness.

### In Scope
1. Premium insights and rules automation baseline.
2. Improved onboarding and activation instrumentation.
3. Performance tuning, crash reduction, and release hardening.
4. Security and privacy release checklist completion.

### Exit Criteria
1. Activation funnel instrumented end-to-end.
2. Priority launch blockers are closed.
3. Beta cohort usage validates core paid value assumptions.

## Phase 6: Launch and Optimization (Weeks 10-12)
### Objective
Launch AU/NZ and optimize conversion and retention.

### In Scope
1. App store launch, operational monitoring, support loop.
2. Weekly onboarding and paywall experiments.
3. Funnel analysis and issue triage cadence.

### Exit Criteria
1. First paying cohort acquired.
2. Conversion and retention metrics are measurable weekly.
3. Post-launch backlog is prioritized by observed behavior.

## Sequencing Rules
1. Do not start Phase 3 before Phase 2 exit criteria are met.
2. Do not start Phase 4 before Phase 3 sync reliability baseline is stable.
3. Keep one phase of overlap only for low-risk parallel tasks (docs, tests, instrumentation).
4. Re-scope aggressively if Phase 1 or Phase 2 exceeds planned duration.
