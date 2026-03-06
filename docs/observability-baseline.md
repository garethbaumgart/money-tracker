# Observability Baseline

## Backend

- Incoming correlation header: `X-Correlation-Id`
- Outgoing correlation header: `X-Correlation-Id`
- Correlation contract:
  - Accept inbound `X-Correlation-Id` when present and valid.
  - Generate a new correlation id when the header is missing or invalid.
  - Echo the effective id in every response header and in problem details.
- Problem detail extension fields:
  - `code`
  - `traceId`
  - `correlationId`
- Standard structured log fields:
  - `Method`
  - `Path`
  - `CorrelationId`
  - `TraceId`
  - `ErrorCode`

Example request without an incoming correlation id:

```text
GET /health
```

Example response headers:

```text
X-Correlation-Id: 9fd6e0c8c0e34c1f9c9e2fce2d42f64d
```

Example problem details extensions on errors:

```text
code: internal_server_error
traceId: 00-1c5cde231efb1a74d6b68b7436f7c2af-5a2f6f2c1094b1bb-01
correlationId: 9fd6e0c8c0e34c1f9c9e2fce2d42f64d
```

## Mobile

- Startup exceptions are captured by `runMoneyTrackerApp` through:
  - `runZonedGuarded` for unhandled async/sync startup failures
  - `FlutterError.onError` for framework-level startup errors
- Reporter contract:
  - Implement `StartupErrorReporter` and inject in bootstrap.
  - `FlutterErrorStartupErrorReporter` is used in `main.dart`; `NoopStartupErrorReporter` is reserved for tests/fallback.
