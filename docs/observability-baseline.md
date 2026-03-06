# Observability Baseline

## Backend

- Incoming correlation header: `X-Correlation-Id`
- Outgoing correlation header: `X-Correlation-Id`
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

## Mobile

- Startup exceptions are captured by `runMoneyTrackerApp` through:
  - `runZonedGuarded` for unhandled async/sync startup failures
  - `FlutterError.onError` for framework-level startup errors
- Reporter contract:
  - Implement `StartupErrorReporter` and inject in bootstrap.
  - The default implementation is `NoopStartupErrorReporter`.
