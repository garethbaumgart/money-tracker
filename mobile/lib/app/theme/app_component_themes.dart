import 'package:flutter/material.dart';

import 'app_theme_tokens.dart';

final class AppComponentThemes {
  AppComponentThemes._();

  static FilledButtonThemeData filledButtons(
    ColorScheme scheme,
    AppThemeTokens tokens,
  ) {
    return FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: scheme.primary,
        foregroundColor: scheme.onPrimary,
        padding: EdgeInsets.symmetric(
          horizontal: tokens.space4,
          vertical: tokens.space3,
        ),
        shape: RoundedRectangleBorder(borderRadius: tokens.radiusMedium),
      ),
    );
  }

  static OutlinedButtonThemeData outlinedButtons(
    ColorScheme scheme,
    AppThemeTokens tokens,
  ) {
    return OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: scheme.onSurface,
        side: BorderSide(color: tokens.borderSubtle),
        padding: EdgeInsets.symmetric(
          horizontal: tokens.space4,
          vertical: tokens.space3,
        ),
        shape: RoundedRectangleBorder(borderRadius: tokens.radiusMedium),
      ),
    );
  }

  static TextButtonThemeData textButtons(
    ColorScheme scheme,
    AppThemeTokens tokens,
  ) {
    return TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: scheme.primary,
        shape: RoundedRectangleBorder(borderRadius: tokens.radiusSmall),
      ),
    );
  }

  static InputDecorationTheme inputDecoration(
    ColorScheme scheme,
    AppThemeTokens tokens,
  ) {
    OutlineInputBorder border(Color color) {
      return OutlineInputBorder(
        borderRadius: tokens.radiusMedium,
        borderSide: BorderSide(color: color),
      );
    }

    return InputDecorationTheme(
      filled: true,
      fillColor: tokens.surfaceMuted,
      contentPadding: EdgeInsets.symmetric(
        horizontal: tokens.space4,
        vertical: tokens.space3,
      ),
      border: border(tokens.borderSubtle),
      enabledBorder: border(tokens.borderSubtle),
      focusedBorder: border(scheme.primary),
      errorBorder: border(tokens.stateDanger),
      focusedErrorBorder: border(tokens.stateDanger),
      hintStyle: TextStyle(color: tokens.contentMuted),
    );
  }

  static CardThemeData cards(ColorScheme scheme, AppThemeTokens tokens) {
    return CardThemeData(
      color: tokens.surfaceElevated,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: tokens.radiusMedium,
        side: BorderSide(color: tokens.borderSubtle),
      ),
      clipBehavior: Clip.antiAlias,
      surfaceTintColor: Colors.transparent,
    );
  }

  static AppBarTheme appBar(ColorScheme scheme, AppThemeTokens tokens) {
    return AppBarTheme(
      backgroundColor: scheme.surface,
      foregroundColor: tokens.contentPrimary,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: false,
      titleTextStyle: TextStyle(
        color: tokens.contentPrimary,
        fontSize: 20,
        fontWeight: FontWeight.w700,
      ),
    );
  }

  static NavigationBarThemeData navigationBar(
    ColorScheme scheme,
    AppThemeTokens tokens,
  ) {
    return NavigationBarThemeData(
      backgroundColor: tokens.surfaceElevated,
      surfaceTintColor: Colors.transparent,
      indicatorColor: Color.lerp(tokens.surfaceElevated, scheme.primary, 0.20),
      labelTextStyle: WidgetStateProperty.resolveWith((states) {
        final isSelected = states.contains(WidgetState.selected);

        return TextStyle(
          color: isSelected ? scheme.primary : tokens.contentSecondary,
          fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
        );
      }),
      iconTheme: WidgetStateProperty.resolveWith((states) {
        final isSelected = states.contains(WidgetState.selected);

        return IconThemeData(
          color: isSelected ? scheme.primary : tokens.contentSecondary,
        );
      }),
    );
  }
}
