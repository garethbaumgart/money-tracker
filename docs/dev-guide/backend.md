# Backend Development Guide

## Canonical Reference

`docs/Backend-DDD-Vertical-Slice-Standards.md`

## Architecture

The backend follows a vertical slice architecture.

New features must start from:

`backend/templates/vertical-slice-template/`

## Layer Responsibilities

### Endpoints

HTTP mapping and DTO translation only.

### Application

Use cases and orchestration logic.

### Domain

Business rules and invariants.

### Infrastructure

Persistence, messaging, and external services.

## Rules

Handlers must remain thin.

Domain invariants must live in domain models.

Repositories and infrastructure code must stay in the infrastructure layer.

## API Contract

Endpoints must follow API conventions defined in:

`docs/App-Build-GuideRails.md`

All API errors must include machine-readable error codes.

OpenAPI specifications must be updated when endpoints change.

## Testing Expectations

Each backend feature should include tests across the relevant layers.

Domain tests  
Application tests  
Integration tests

Contract validation tests when applicable.
