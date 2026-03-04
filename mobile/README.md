# Money Tracker Mobile

Flutter mobile shell for the Money Tracker app.

## Commands

```bash
flutter pub get
flutter analyze
flutter test
flutter run
```

## Notes

- Package name: `money_tracker`

## Configuring App Namespace / Bundle ID

When you move this app under an organization account, update these config files:

- Android: `android/gradle.properties`
  - `app.namespace`
  - `app.applicationId`
  - `app.mainActivityClass` (only if you also move the Kotlin package for `MainActivity`)
- iOS: `ios/Flutter/AppConfig.xcconfig`
  - `APP_BUNDLE_ID`
