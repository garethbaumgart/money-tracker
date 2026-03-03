---
name: backend-ddd-vertical-slice
description: Implement backend features with vertical slices and pragmatic Domain-Driven Design in .NET. Use when adding or refactoring API endpoints, commands/queries, domain rules, repositories, integrations, or tests for business-critical modules such as budgets, transactions, households, subscriptions, and provider sync flows.
---

# Backend Ddd Vertical Slice

## Overview

Build backend features as independent slices with clear boundaries.
Model rich domain logic only where business rules are non-trivial.

## Workflow

1. Identify the target module and business capability.
2. Create or update the vertical slice folders: `Domain`, `Application`, `Infrastructure`, `Presentation`.
3. Put invariants and business rules in domain entities/value objects.
4. Keep command/query handlers orchestration-focused.
5. Keep endpoint handlers thin and contract-driven.
6. Add integration boundaries for persistence and external systems.
7. Add tests at domain, application, integration, and endpoint levels.
8. Verify OpenAPI and idempotency implications before completion.

## Decision Rules

1. Use rich domain models for core business rules.
2. Use simple DTO/projection queries for read-heavy endpoints.
3. Keep domain independent of web, DB, and provider SDK concerns.
4. Keep each handler focused on one use case.
5. Prefer explicit domain errors over generic exceptions.
6. Keep cross-module coupling via interfaces and contracts only.

## Required Output Elements

1. Slice architecture summary.
2. Domain model and invariants summary.
3. API contract changes and OpenAPI impact.
4. Persistence/integration boundaries changed.
5. Test evidence by layer.
6. Risk and rollout notes.

## Verification Checklist

1. Domain layer has no framework dependencies.
2. Endpoint code contains no business rule logic.
3. Command/query handlers are thin orchestration.
4. Webhook/provider flows are idempotent and observable.
5. OpenAPI is updated for contract changes.
6. Critical paths have tests across required layers.

## Reference Files

1. Backend conventions and templates:
- `references/backend-ddd-vertical-slice-checklist.md`

Load this checklist before implementing backend features.
