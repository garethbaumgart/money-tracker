# Money Tracker

Phase 1 foundation repo for the Money Tracker mobile app and backend API.

## Prerequisites

- Flutter 3.32+ with Dart 3.8+
- .NET SDK 10.0+

## Project Layout

```text
mobile/   Flutter app shell
backend/  .NET API shell and templates
docs/     Architecture and execution guides
skills/   Codex workflow skills
```

## Mobile Shell

From repo root:

```bash
cd mobile
flutter pub get
flutter analyze
flutter test
flutter run
```

## Backend API Shell

From repo root:

```bash
dotnet restore backend/MoneyTracker.slnx
dotnet build backend/MoneyTracker.slnx
dotnet run --project backend/src/MoneyTracker.Api/MoneyTracker.Api.csproj
```

## Notes

- Backend solution uses `.slnx` (default format for current .NET SDK).
- Feature implementation standards are defined in:
  - `docs/App-Build-GuideRails.md`
  - `docs/Backend-DDD-Vertical-Slice-Standards.md`
  - `docs/Flutter-UX-Theming-Standards.md`
