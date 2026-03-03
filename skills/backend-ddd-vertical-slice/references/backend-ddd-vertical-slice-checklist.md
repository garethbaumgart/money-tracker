# Backend DDD Vertical Slice Checklist

## 1. Slice Setup
- Place code in the correct module under `backend/src/Modules/<Feature>/`.
- Keep `Domain`, `Application`, `Infrastructure`, and `Presentation` boundaries clear.
- Avoid cross-feature direct references unless contractually required.

## 2. Domain Modeling
- Model invariants in entities/value objects, not handlers.
- Keep domain constructors/factories validating business rules.
- Use explicit domain errors for business failures.
- Keep domain free from framework, DB, and HTTP dependencies.

## 3. Application Layer
- Use one command/query handler per use case.
- Keep handlers focused on orchestration and transaction boundaries.
- Validate input shape before domain execution.
- Emit domain/application events where needed for side effects.

## 4. Presentation Layer
- Keep endpoints/controllers thin.
- Map transport DTOs to command/query objects.
- Keep endpoint responses contract-driven and explicit.
- Return machine-readable error codes for known failure modes.

## 5. Infrastructure Layer
- Keep repository/provider implementations out of domain/application.
- Keep provider calls resilient: timeout, retry strategy, idempotency.
- Ensure webhook/event ingestion is idempotent and signature-validated.
- Add structured logs and correlation IDs for critical flows.

## 6. API Contract
- Update OpenAPI in the same change as endpoint contract updates.
- Document backward compatibility and migration impact.
- Avoid breaking changes without explicit rollout and versioning plan.

## 7. Testing
- Domain tests for invariants and rule enforcement.
- Application tests for handler orchestration.
- Integration tests for DB/provider/webhook behavior.
- Endpoint tests for request/response contracts.
- E2E tests for critical business flows.

## 8. Done Criteria
- Business logic is in domain, not presentation.
- OpenAPI and docs updated.
- Required tests added and passing.
- Risk and rollback notes included for high-impact changes.
