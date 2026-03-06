# Environment Configuration Baseline

This document defines the required configuration contract for Local, Staging, and Production environments.

## Environment Names

Backend and mobile use these canonical environment names:

1. `Local`
2. `Staging`
3. `Production`

`Testing` is reserved for automated test hosts only.

## Required Keys Matrix

| Scope | Key | Local | Staging | Production | Notes |
| --- | --- | --- | --- | --- | --- |
| Backend | `Api:ServiceName` | Required | Required | Required | Stable service identifier in logs and diagnostics. |
| Backend | `Api:Environment` | Required | Required | Required | Must be `Local`, `Staging`, `Production`, or `Testing`. |
| Backend | `Database:ConnectionString` | Optional | Required | Required | Required only for staging/production startup validation. |
| Backend | `Observability:ErrorReporterDsn` | Optional | Optional | Optional | Provider-specific reporter DSN. |
| Mobile (`--dart-define`) | `APP_ENV` | Required | Required | Required | Accepted values: `local`, `staging`, `production`. |
| Mobile (`--dart-define`) | `API_BASE_URL` | Required | Required | Required | Must be an absolute URL. |
| Mobile (`--dart-define`) | `ERROR_REPORTING_DSN` | Optional | Optional | Optional | Optional provider DSN for startup reporting. |

## Backend Files

1. `backend/src/MoneyTracker.Api/appsettings.json` (baseline production defaults with placeholders)
2. `backend/src/MoneyTracker.Api/appsettings.Local.json`
3. `backend/src/MoneyTracker.Api/appsettings.Staging.json`
4. `backend/src/MoneyTracker.Api/appsettings.Development.json` (development alias mapped to Local)

## Mobile Build Examples

Local run:

```bash
flutter run \
  --dart-define=APP_ENV=local \
  --dart-define=API_BASE_URL=https://api.local.money-tracker.test
```

Staging run:

```bash
flutter run \
  --dart-define=APP_ENV=staging \
  --dart-define=API_BASE_URL=https://api.staging.money-tracker.example
```

## Secret Handling Boundaries

1. Never commit real secrets to the repository.
2. Only placeholder/example values belong in committed files.
3. Real staging/production secrets must be injected by CI/runtime secret stores.
4. Keep `.env` files local-only; use `.env.example` files for shape documentation.
