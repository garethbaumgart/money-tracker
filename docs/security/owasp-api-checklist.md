# OWASP API Security Top 10 Checklist

Status key: [x] Implemented | [ ] Not applicable | [~] Partial

## API1:2023 - Broken Object Level Authorization

- [x] All endpoints validate user ownership or membership before returning data
- [x] Data export endpoint enforces self-only access (403 for non-matching userId)
- [x] Account deletion endpoint enforces self-only access (403 for non-matching userId)
- [x] Household endpoints verify membership via IHouseholdAccessService

## API2:2023 - Broken Authentication

- [x] Challenge-based authentication with short-lived tokens (10 min)
- [x] Access tokens expire after 15 minutes
- [x] Refresh tokens expire after 7 days
- [x] Token rotation on refresh (old tokens invalidated)
- [x] Rate limiting on auth endpoints (10 req/min/IP)

## API3:2023 - Broken Object Property Level Authorization

- [x] Bank connection export excludes sensitive fields (ExternalConnectionId, ConsentSessionId)
- [x] API responses use explicit DTOs, not raw domain entities

## API4:2023 - Unrestricted Resource Consumption

- [x] Rate limiting per endpoint group with sliding window
- [x] Payload size limit (1MB) enforced via PayloadSizeLimitMiddleware
- [x] Per-group rate limits (Auth: 10/min, CRUD: 60/min, Bank: 20/min, etc.)

## API5:2023 - Broken Function Level Authorization

- [x] Admin endpoints protected by IAdminAccessService
- [x] Rate limiting applied differently for admin endpoints (30 req/min)
- [x] Webhook endpoints use signature validation

## API6:2023 - Unrestricted Access to Sensitive Business Flows

- [x] Challenge attempt limit (5 max attempts per challenge)
- [x] Rate limiting on authentication endpoints
- [x] Bank connection link sessions require authentication

## API7:2023 - Server Side Request Forgery (SSRF)

- [x] External API calls (Basiq) use configured base URLs, not user-supplied URLs
- [x] HTTP client timeouts configured (30s)

## API8:2023 - Security Misconfiguration

- [x] Security headers on all responses (HSTS, X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy)
- [x] Cache-Control: no-store on all API responses
- [x] OpenAPI spec only exposed in Development/Testing environments
- [x] Detailed error messages suppressed in production via GlobalExceptionHandler

## API9:2023 - Improper Inventory Management

- [x] All endpoints registered with OpenAPI metadata (WithName, WithSummary, WithDescription)
- [x] OpenAPI spec generated and available in non-production environments

## API10:2023 - Unsafe Consumption of APIs

- [x] Basiq webhook signatures validated before processing
- [x] RevenueCat webhook signatures validated before processing
- [x] Input validation helpers enforce max length and reject control characters
