# Flutter UX and Theming Standards (v1)

## Purpose
Define how Flutter UI should be built so styling stays consistent, composable, and easy to evolve.

## Principles
1. Theme first, feature second.
2. Compose Material components before creating custom components.
3. Keep tokens semantic and centrally managed.
4. Keep per-screen overrides rare and explicit.
5. Optimize for maintainability and predictable iteration speed.

## Theme Architecture
Use a single entry point for theme construction in `mobile/lib/app/theme/`.

Recommended structure:

```text
mobile/lib/app/theme/
  app_theme.dart
  app_color_schemes.dart
  app_text_theme.dart
  app_component_themes.dart
  app_tokens.dart
```

`app_theme.dart` should expose:
- `ThemeData lightTheme`
- `ThemeData darkTheme`
- `ThemeMode defaultThemeMode`

## Material 3 Baseline
1. Use `ThemeData` + `ColorScheme` as the core.
2. Prefer `ColorScheme.fromSeed` for coherent color roles.
3. Drive components from the color scheme instead of direct color constants.
4. Keep app-level component theme defaults in one place.

## Tokenization Strategy
Use two token layers:
1. Material tokens:
- `ColorScheme`, `TextTheme`, built-in component theme data.
2. App semantic tokens via `ThemeExtension`:
- custom semantic colors
- spacing scale
- corner radius scale
- elevations and shadows

Example extension:

```dart
@immutable
class AppTokens extends ThemeExtension<AppTokens> {
  const AppTokens({
    required this.spaceSm,
    required this.spaceMd,
    required this.radiusMd,
    required this.contentMuted,
  });

  final double spaceSm;
  final double spaceMd;
  final BorderRadius radiusMd;
  final Color contentMuted;

  @override
  AppTokens copyWith({
    double? spaceSm,
    double? spaceMd,
    BorderRadius? radiusMd,
    Color? contentMuted,
  }) {
    return AppTokens(
      spaceSm: spaceSm ?? this.spaceSm,
      spaceMd: spaceMd ?? this.spaceMd,
      radiusMd: radiusMd ?? this.radiusMd,
      contentMuted: contentMuted ?? this.contentMuted,
    );
  }

  @override
  AppTokens lerp(ThemeExtension<AppTokens>? other, double t) {
    if (other is! AppTokens) return this;
    return AppTokens(
      spaceSm: lerpDouble(spaceSm, other.spaceSm, t)!,
      spaceMd: lerpDouble(spaceMd, other.spaceMd, t)!,
      radiusMd: BorderRadius.lerp(radiusMd, other.radiusMd, t)!,
      contentMuted: Color.lerp(contentMuted, other.contentMuted, t)!,
    );
  }
}
```

## Component Composition Strategy
1. Use Material components as the default building blocks.
2. Use thin wrappers only for repeated domain semantics.
3. Wrappers must read from theme/tokens, never hardcode palette values.

Good wrappers:
- `AppPrimaryButton`
- `AppMoneyField`
- `AppBudgetCard`

Bad wrappers:
- One-off wrappers used by a single screen only.
- Wrappers that bypass theme values and hardcode styles.

## Inline Styling Policy
Allowed:
1. Temporary experimentation behind a short-lived branch.
2. Contextual exceptions with a documented reason.

Not allowed:
1. Hardcoded colors in feature widgets when equivalent token exists.
2. Duplicating style objects across multiple screens.
3. New typography scales defined ad hoc in feature code.

## Adaptive Layout Rules
1. Branch layout by available width using `LayoutBuilder` or `MediaQuery.sizeOf`.
2. Do not branch by device model or platform heuristics.
3. Validate compact and expanded layouts for key screens.

## Accessibility Requirements
1. Validate readable contrast for text and controls.
2. Ensure disabled, focused, pressed, and error states are distinct.
3. Keep target sizes and spacing accessible.
4. Validate with larger text scale and screen reader semantics.

## Testing and Verification
1. Add widget tests for theme-sensitive states when logic is non-trivial.
2. Add golden tests for critical surfaces with high visual regression risk.
3. Verify light and dark variants for every theme-affecting change.
4. Include screenshots or evidence in PRs for major UI changes.

## Delivery Checklist
1. Theme changes are centralized in theme/token files.
2. Feature UI consumes theme and token APIs only.
3. Inline style debt was removed or tracked with follow-up.
4. Light/dark/system behavior validated.
5. Accessibility and responsive checks completed.

## Links to Project Controls
1. Build policy: `docs/App-Build-GuideRails.md`
2. Runtime skill workflow: `skills/flutter-ux-theming/SKILL.md`
3. UX mockup workflow: `docs/UX-Mockup-Workflow.md`
4. Agent trigger rules: `AGENTS.md`
