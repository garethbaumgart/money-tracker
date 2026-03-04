import 'package:flutter/material.dart';

import 'app_component_themes.dart';
import 'app_theme_tokens.dart';

final class MoneyTrackerTheme {
  MoneyTrackerTheme._();

  static ThemeData light() => _buildTheme(_lightScheme);

  static ThemeData dark() => _buildTheme(_darkScheme);

  static ThemeData _buildTheme(ColorScheme scheme) {
    final tokens = AppThemeTokens.fromColorScheme(scheme);
    final base = ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      brightness: scheme.brightness,
    );

    return base.copyWith(
      scaffoldBackgroundColor: tokens.background,
      textTheme: base.textTheme.apply(
        bodyColor: tokens.contentPrimary,
        displayColor: tokens.contentPrimary,
      ),
      filledButtonTheme: AppComponentThemes.filledButtons(scheme, tokens),
      outlinedButtonTheme: AppComponentThemes.outlinedButtons(scheme, tokens),
      textButtonTheme: AppComponentThemes.textButtons(scheme, tokens),
      inputDecorationTheme: AppComponentThemes.inputDecoration(scheme, tokens),
      cardTheme: AppComponentThemes.cards(scheme, tokens),
      appBarTheme: AppComponentThemes.appBar(scheme, tokens),
      navigationBarTheme: AppComponentThemes.navigationBar(scheme, tokens),
      extensions: <ThemeExtension<dynamic>>[tokens],
    );
  }

  static final ColorScheme _lightScheme =
      ColorScheme.fromSeed(
        seedColor: const Color(0xFF005AC1),
        brightness: Brightness.light,
      ).copyWith(
        primary: const Color(0xFF005AC1),
        onPrimary: const Color(0xFFFFFFFF),
        secondary: const Color(0xFF415977),
        onSecondary: const Color(0xFFFFFFFF),
        error: const Color(0xFFA3261A),
        onError: const Color(0xFFFFFFFF),
        surface: const Color(0xFFFFFFFF),
        onSurface: const Color(0xFF142238),
      );

  static final ColorScheme _darkScheme =
      ColorScheme.fromSeed(
        seedColor: const Color(0xFF5EA3FF),
        brightness: Brightness.dark,
      ).copyWith(
        primary: const Color(0xFF5EA3FF),
        onPrimary: const Color(0xFF081324),
        secondary: const Color(0xFFC0D2EC),
        onSecondary: const Color(0xFF0B111B),
        error: const Color(0xFFFF8A80),
        onError: const Color(0xFF320001),
        surface: const Color(0xFF121D2D),
        onSurface: const Color(0xFFEBF2FF),
      );
}
