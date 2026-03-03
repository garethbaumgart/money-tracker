# App Build GuideRails (Locked v1)

## Purpose
1. Define non-negotiable engineering defaults for this project.
2. Keep implementation quality high with a 1-2 builder team.
3. Minimize architecture drift and delivery friction.

## Technology Baseline
1. Mobile: Flutter (Dart 3+).
2. Backend: C# on .NET 10, deployed to Cloud Run.
3. Data: Neon PostgreSQL.
4. API: REST with OpenAPI-first contracts.
5. Billing: RevenueCat.
6. Bank integration: Basiq first, Akahu-compatible adapter retained for fallback.

## Architecture Rules
1. Use vertical slices by feature, not global technical layers.
2. Within each slice, enforce DDD boundaries:
- `domain`: entities, value objects, domain services, repository interfaces.
- `application`: commands, queries, handlers, orchestration.
- `infrastructure`: API/data/provider implementations.
- `presentation`: view-models, screens, widgets, route wiring.
3. Shared kernel is limited to stable cross-cutting primitives:
- `Money`, `Currency`, IDs, typed errors, events, and shared policies.
4. Domain layer must not import infrastructure or presentation code.
5. External systems must be isolated through anti-corruption interfaces:
- `IBankProviderAdapter`
- `ISubscriptionEntitlementService`

## Recommended Repository Layout
```text
mobile/
  lib/
    app/
    shared_kernel/
    features/
      households/
        domain/
        application/
        infrastructure/
        presentation/
      budgets/
      transactions/
      bank_connections/
      subscriptions/
      insights/
backend/
  src/
    Modules/
      Households/
      Budgets/
      Transactions/
      BankConnections/
      Subscriptions/
  tests/
docs/
```

## Coding Standards
1. Prefer immutable models and explicit constructors for domain objects.
2. Keep command/query handlers focused on one use case each.
3. Ban shared utility "grab bags"; create typed modules with clear ownership.
4. Log with correlation IDs for all async/sync orchestration paths.
5. Handle all network/provider calls with timeout, retry, and idempotency keys.
6. Keep feature flags server-backed for monetization and provider-sensitive flows.
7. For breaking decisions, require an ADR with owner and expiry date.

## Flutter UX and Theming Guardrails
1. For Flutter UI/theming work, apply `skills/flutter-ux-theming/SKILL.md`.
2. Follow the detailed standard in `docs/Flutter-UX-Theming-Standards.md`.
3. Keep UI style decisions tokenized and centralized in theme files.
4. Prefer Material component composition over one-off custom widgets.
5. For UX-heavy issues, require `skills/ux-mockup-explorer/SKILL.md` decision pack before implementation.
6. Store mockups in `docs/ux-mockups/<issue-id>-<slug>/` and record selected option.

## Backend DDD and Vertical Slice Guardrails
1. For backend/API feature work, apply `skills/backend-ddd-vertical-slice/SKILL.md`.
2. Follow the detailed standard in `docs/Backend-DDD-Vertical-Slice-Standards.md`.
3. Keep business invariants in domain models, not endpoints.
4. Keep handlers and endpoints thin; keep infrastructure concerns isolated.
5. Start new backend features from `backend/templates/vertical-slice-template/`.

## API and Contract Standards
1. OpenAPI changes are required in the same PR as endpoint changes.
2. Endpoint naming must be resource-oriented and version-safe.
3. Required baseline endpoints:
- `POST /auth/*`
- `POST /households`
- `POST /households/{id}/invite`
- `GET /transactions`
- `POST /transactions`
- `POST /bank/link-session`
- `POST /bank/callback`
- `POST /bank/sync`
- `GET /subscriptions/entitlements`
- `POST /subscriptions/restore`
- `POST /webhooks/revenuecat`
4. API responses must include machine-readable error codes.
5. All webhook handlers must be idempotent and signature-validated.

## Domain and Type Contracts
1. Mobile domain types:
- `SubscriptionTier`
- `FeatureKey`
- `HouseholdRole`
- `ConsentStatus`
2. Mobile gateway contracts:
- `BankConnectionGateway`
- `SubscriptionGateway`
- `HouseholdGateway`
- `BudgetGateway`
3. Backend interfaces:
- `IBankProviderAdapter` with Basiq implementation and Akahu-compatible abstraction.
- `ISubscriptionEntitlementService` for RevenueCat entitlements.

## Testing Standards (Heavy Coverage Bar)
1. Domain tests:
- Budget math, rollover behavior, permissions, and invariants.
2. Application tests:
- Command/query handlers for invite, budget creation, bank sync orchestration.
3. Integration tests:
- Neon persistence, webhook idempotency, provider sync dedupe.
4. Contract tests:
- OpenAPI compatibility for all mobile-consumed endpoints.
5. End-to-end tests:
- Signup to first budget.
- Bank link to first synced transaction.
- Trial start to paid entitlement.
- Partner invite to shared dashboard.
6. Non-functional tests:
- Backoff behavior on provider errors.
- Observability checks (structured logs, trace IDs, alertability).

## Coverage and Quality Gates
1. Backend minimum coverage:
- Domain + application code: 80 percent line coverage.
2. Mobile minimum coverage:
- Domain + application logic: 70 percent line coverage.
3. Critical-path E2E suite:
- Must pass on release branches and production promotions.
4. Security gates:
- Dependency vulnerability scan with fail-on-high.
- Secret scanning enabled on CI.

## CI/CD Standards
1. Required PR checks:
- Build, tests, lint/static analysis, OpenAPI diff, security scan.
2. Environment promotion flow:
- `dev` -> `staging` -> `prod`.
3. Each promotion requires automated smoke checks.
4. DB migrations:
- Backward-compatible by default.
- Destructive migrations require phased rollout and rollback plan.

## Git Workflow
1. Use trunk-based development with short-lived branches.
2. Name branches with `codex/<scope>-<short-description>`.
3. Keep PR size under ~400 net lines where possible.
4. Require at least one reviewer for backend and API contract changes.
5. Require test evidence in PR description.

## Definition of Done
1. Business acceptance criteria met.
2. OpenAPI and interface contracts updated.
3. Required tests added and green.
4. Observability added for new async or provider flows.
5. Documentation updated where behavior changed.
6. Feature flag rollout and rollback plan documented when relevant.

## Assumptions and Defaults
1. Team size is 1-2 builders.
2. .NET 10 is accepted runtime baseline.
3. Cloud Run + Neon is the default backend platform path.
4. REST + OpenAPI is the integration contract baseline.
