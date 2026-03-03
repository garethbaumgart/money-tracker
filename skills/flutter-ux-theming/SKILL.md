---
name: flutter-ux-theming
description: Apply consistent Flutter UX and theming standards using Material 3, ThemeData, ColorScheme, ThemeExtension tokens, and component themes. Use when building or refactoring Flutter screens, introducing design tokens, changing theme behavior, adding dark/light modes, or reviewing UI code for theming consistency and reuse.
---

# Flutter Ux Theming

## Overview

Produce UI changes that are token-driven, theme-first, and component-composed.
Avoid one-off styling and custom component sprawl.

## Workflow

1. Identify the current theme entry points and component usage.
2. Normalize theme structure around `ThemeData`, `ColorScheme`, and component themes.
3. Move custom semantic tokens into `ThemeExtension` types.
4. Replace inline styles with theme-aware composition.
5. Validate light, dark, and system behavior paths.
6. Confirm accessibility and responsive behavior.
7. Output changed files plus a brief verification checklist.

## Decision Rules

1. Prefer Material components before custom widgets.
2. Prefer composing existing widgets before creating new abstractions.
3. Use custom wrappers only for repeated behavior or strict product semantics.
4. Keep tokens semantic and intent-based, not raw color names.
5. Keep design primitives centrally defined and versioned.
6. Keep per-screen overrides minimal and local.

## Required Output Elements

1. Theme architecture summary.
2. Token model summary.
3. Component composition strategy.
4. List of inline-style removals or remaining exceptions.
5. Accessibility and responsive checks run.

## Verification Checklist

1. `theme`, `darkTheme`, and `themeMode` are all configured.
2. `ColorScheme` is the source of truth for component colors.
3. `ThemeExtension` is used for non-Material semantic tokens.
4. Component themes are used for buttons, fields, cards, app bars, and nav bars.
5. No uncontrolled hardcoded colors or text styles remain in changed features.
6. Large-screen layout behavior is based on available width, not device type checks.

## Reference Files

1. Standards and checklist:
- `references/flutter-ux-theming-checklist.md`

Load this file for detailed rules before implementing Flutter UI/theming changes.
