# Mobile Development Guide

## Canonical Reference

`docs/App-Build-GuideRails.md`

## UI Architecture

UI should follow the Flutter UX and theming standards.

Use centralized theme configuration rather than local styling.

## Tokens

Spacing  
Radius  
Content roles

These should be defined using ThemeExtension or equivalent token structures.

## UI Composition

Prefer Material components and composition over custom widgets.

Avoid hardcoded colors, spacing, or typography.

## UX Workflow

For UX-heavy tasks:

1. Generate a UX option pack.
2. Select the chosen option.
3. Create `selected.txt`.
4. Only then implement the UI.

## Testing Expectations

Widget tests for UI behavior.

Integration tests for navigation and app startup flows.
