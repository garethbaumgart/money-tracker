# Flutter UX Theming Checklist

## 1. Theme Foundation
- Use Material 3 (`useMaterial3: true` if needed by target SDK defaults).
- Configure `theme`, `darkTheme`, and `themeMode`.
- Build colors from `ColorScheme` (`fromSeed` or an explicit scheme).
- Avoid legacy accent/swatch patterns for new work.

## 2. Token Strategy
- Keep tokens semantic: `contentPrimary`, `surfaceMuted`, `danger`.
- Avoid raw token names tied to hue like `blue500`.
- Put non-Material tokens in a `ThemeExtension`.
- Keep spacing, radius, and elevation scales centralized.

## 3. Component Theming
- Define app-level themes for:
- `FilledButtonThemeData`
- `OutlinedButtonThemeData`
- `TextButtonThemeData`
- `InputDecorationTheme`
- `CardThemeData`
- `AppBarThemeData`
- `NavigationBarThemeData`
- Avoid inline style duplication in screens.

## 4. Composition Over Custom Components
- Compose Material widgets first.
- Add wrappers only when behavior is repeated in 3+ places or domain semantics are needed.
- Keep wrappers thin and theme-aware.

## 5. Typography
- Start from `TextTheme` and only override intentionally.
- Keep scale consistent across light and dark themes.
- Avoid hardcoded font sizes in feature code unless exception is documented.

## 6. Accessibility
- Verify contrast for text and interactive states.
- Ensure disabled, focused, pressed, and error states are visually distinct.
- Support larger text and avoid clipped layouts at high text scale.
- Ensure touch targets are not too small.

## 7. Responsive Behavior
- Use `LayoutBuilder` or available-width checks.
- Avoid hardcoding device-type assumptions.
- Validate navigation and density for compact and expanded layouts.

## 8. State and Feedback
- Ensure loading, empty, success, and error states are themed consistently.
- Centralize snackbar/dialog/bottom-sheet style.
- Keep destructive actions visually distinct and confirmable.

## 9. Engineering Checks
- Remove hardcoded colors and ad hoc styles from changed files.
- Keep all theming changes in dedicated theme/token files when possible.
- Add snapshot/golden tests where visual regression risk is high.
- Document any temporary style exceptions and expiry conditions.

## 10. Done Criteria
- Theme architecture and tokens are documented.
- New UI uses tokens and component themes.
- Light/dark/system behavior validated.
- Accessibility and responsive checks completed.
