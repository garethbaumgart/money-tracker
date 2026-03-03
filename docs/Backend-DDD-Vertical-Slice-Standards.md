# Backend DDD + Vertical Slice Standards (v1)

## Purpose
Define consistent backend conventions for building and evolving API features with pragmatic DDD and vertical slices.

## Scope
Applies to all backend modules in `backend/src/Modules/*`.

## Core Principles
1. Organize by feature/capability, not technical layer at repository root.
2. Keep domain rules close to domain models.
3. Keep handlers and endpoints thin.
4. Keep infrastructure concerns isolated behind interfaces.
5. Keep contracts explicit and testable.

## Module Structure

```text
backend/src/Modules/<Feature>/
  Domain/
  Application/
    <UseCase>/
  Infrastructure/
  Presentation/
```

## Starter Template
Use this starter when creating a new backend feature slice:

1. `backend/templates/vertical-slice-template/`
2. Follow `backend/templates/vertical-slice-template/USAGE.md`

## Domain Standards
1. Use entities/value objects for business invariants.
2. Keep domain constructors/factories validating core rules.
3. Represent money, status, and identifiers with typed models where practical.
4. Do not reference HTTP, ORM, SDK, or framework types in domain classes.
5. Use explicit domain errors for business rule violations.

## Application Standards
1. Model each write use case as a command + handler.
2. Model each read use case as a query + handler or projection path.
3. Keep handlers orchestration-focused.
4. Keep transaction boundaries explicit.
5. Keep idempotency explicit for externally-triggered writes.

## Presentation Standards
1. Keep endpoint/controller methods thin.
2. Map request DTOs to command/query contracts.
3. Keep response DTOs explicit and stable.
4. Return machine-readable error codes.
5. Keep auth and validation concerns explicit and composable.

## Infrastructure Standards
1. Implement repository and provider interfaces here only.
2. Keep external provider adapters isolated.
3. Use timeout, retry, and backoff for provider calls.
4. Add structured logs and correlation IDs for async flows.
5. Keep webhook ingestion idempotent and signature-validated.

## Pragmatic DDD Rules
1. Use rich domain modeling for complex rules.
2. Use direct projections for simple read models.
3. Avoid abstract base-class hierarchies that add little value.
4. Avoid introducing aggregates/entities where plain query DTOs are enough.
5. Prefer simple, explicit code over pattern purity.

## API Contract and Versioning
1. Keep REST + OpenAPI as contract baseline.
2. Update OpenAPI in the same PR as endpoint changes.
3. Document compatibility and migration behavior.
4. Use feature flags or phased rollouts for risky behavior changes.

## Testing Expectations
1. Domain tests for invariants and value semantics.
2. Application tests for use-case orchestration.
3. Integration tests for DB and provider boundaries.
4. Contract tests for endpoint schemas.
5. E2E tests for critical revenue and money flows.

## Observability and Reliability
1. Track correlation IDs across async boundaries.
2. Emit structured logs for provider calls and webhook processing.
3. Define alerts for sync failures and entitlement drift.
4. Record retries and dead-letter behavior where applicable.

## Delivery Checklist
1. Slice boundaries respected.
2. Domain logic is not in endpoint or repository glue code.
3. OpenAPI and docs updated.
4. Tests added and green for affected layers.
5. Rollback/risk notes included for high-impact changes.

## Links to Project Controls
1. Build policy: `docs/App-Build-GuideRails.md`
2. Skill workflow: `skills/backend-ddd-vertical-slice/SKILL.md`
3. Agent routing: `AGENTS.md`
