import 'dart:ui' show lerpDouble;

import 'package:flutter/material.dart';

@immutable
class AppThemeTokens extends ThemeExtension<AppThemeTokens> {
  const AppThemeTokens({
    required this.background,
    required this.surfaceMuted,
    required this.surfaceElevated,
    required this.borderSubtle,
    required this.contentPrimary,
    required this.contentSecondary,
    required this.contentMuted,
    required this.stateSuccess,
    required this.stateWarning,
    required this.stateDanger,
    required this.space1,
    required this.space2,
    required this.space3,
    required this.space4,
    required this.space5,
    required this.space6,
    required this.radiusSmall,
    required this.radiusMedium,
    required this.radiusLarge,
  });

  factory AppThemeTokens.fromColorScheme(ColorScheme scheme) {
    final isDark = scheme.brightness == Brightness.dark;

    return AppThemeTokens(
      background: isDark ? const Color(0xFF0B111B) : const Color(0xFFEEF3FB),
      surfaceMuted: isDark ? const Color(0xFF152338) : const Color(0xFFF4F7FC),
      surfaceElevated: isDark
          ? const Color(0xFF172942)
          : const Color(0xFFFBFDFF),
      borderSubtle: isDark ? const Color(0xFF274262) : const Color(0xFFD5DFEF),
      contentPrimary: isDark
          ? const Color(0xFFEBF2FF)
          : const Color(0xFF142238),
      contentSecondary: isDark
          ? const Color(0xFFC0D2EC)
          : const Color(0xFF415977),
      contentMuted: isDark ? const Color(0xFF8EA7C9) : const Color(0xFF6F839C),
      stateSuccess: isDark ? const Color(0xFF58C58F) : const Color(0xFF177245),
      stateWarning: isDark ? const Color(0xFFFFBC5B) : const Color(0xFF9A5B00),
      stateDanger: isDark ? const Color(0xFFFF8A80) : const Color(0xFFA3261A),
      space1: 4,
      space2: 8,
      space3: 12,
      space4: 16,
      space5: 24,
      space6: 32,
      radiusSmall: BorderRadius.circular(8),
      radiusMedium: BorderRadius.circular(14),
      radiusLarge: BorderRadius.circular(22),
    );
  }

  final Color background;
  final Color surfaceMuted;
  final Color surfaceElevated;
  final Color borderSubtle;
  final Color contentPrimary;
  final Color contentSecondary;
  final Color contentMuted;
  final Color stateSuccess;
  final Color stateWarning;
  final Color stateDanger;

  final double space1;
  final double space2;
  final double space3;
  final double space4;
  final double space5;
  final double space6;

  final BorderRadius radiusSmall;
  final BorderRadius radiusMedium;
  final BorderRadius radiusLarge;

  static AppThemeTokens of(BuildContext context) {
    final tokens = Theme.of(context).extension<AppThemeTokens>();
    if (tokens == null) {
      throw StateError('AppThemeTokens are missing from ThemeData.extensions.');
    }

    return tokens;
  }

  @override
  AppThemeTokens copyWith({
    Color? background,
    Color? surfaceMuted,
    Color? surfaceElevated,
    Color? borderSubtle,
    Color? contentPrimary,
    Color? contentSecondary,
    Color? contentMuted,
    Color? stateSuccess,
    Color? stateWarning,
    Color? stateDanger,
    double? space1,
    double? space2,
    double? space3,
    double? space4,
    double? space5,
    double? space6,
    BorderRadius? radiusSmall,
    BorderRadius? radiusMedium,
    BorderRadius? radiusLarge,
  }) {
    return AppThemeTokens(
      background: background ?? this.background,
      surfaceMuted: surfaceMuted ?? this.surfaceMuted,
      surfaceElevated: surfaceElevated ?? this.surfaceElevated,
      borderSubtle: borderSubtle ?? this.borderSubtle,
      contentPrimary: contentPrimary ?? this.contentPrimary,
      contentSecondary: contentSecondary ?? this.contentSecondary,
      contentMuted: contentMuted ?? this.contentMuted,
      stateSuccess: stateSuccess ?? this.stateSuccess,
      stateWarning: stateWarning ?? this.stateWarning,
      stateDanger: stateDanger ?? this.stateDanger,
      space1: space1 ?? this.space1,
      space2: space2 ?? this.space2,
      space3: space3 ?? this.space3,
      space4: space4 ?? this.space4,
      space5: space5 ?? this.space5,
      space6: space6 ?? this.space6,
      radiusSmall: radiusSmall ?? this.radiusSmall,
      radiusMedium: radiusMedium ?? this.radiusMedium,
      radiusLarge: radiusLarge ?? this.radiusLarge,
    );
  }

  @override
  AppThemeTokens lerp(
    covariant ThemeExtension<AppThemeTokens>? other,
    double t,
  ) {
    if (other is! AppThemeTokens) {
      return this;
    }

    return AppThemeTokens(
      background: Color.lerp(background, other.background, t)!,
      surfaceMuted: Color.lerp(surfaceMuted, other.surfaceMuted, t)!,
      surfaceElevated: Color.lerp(surfaceElevated, other.surfaceElevated, t)!,
      borderSubtle: Color.lerp(borderSubtle, other.borderSubtle, t)!,
      contentPrimary: Color.lerp(contentPrimary, other.contentPrimary, t)!,
      contentSecondary: Color.lerp(
        contentSecondary,
        other.contentSecondary,
        t,
      )!,
      contentMuted: Color.lerp(contentMuted, other.contentMuted, t)!,
      stateSuccess: Color.lerp(stateSuccess, other.stateSuccess, t)!,
      stateWarning: Color.lerp(stateWarning, other.stateWarning, t)!,
      stateDanger: Color.lerp(stateDanger, other.stateDanger, t)!,
      space1: lerpDouble(space1, other.space1, t)!,
      space2: lerpDouble(space2, other.space2, t)!,
      space3: lerpDouble(space3, other.space3, t)!,
      space4: lerpDouble(space4, other.space4, t)!,
      space5: lerpDouble(space5, other.space5, t)!,
      space6: lerpDouble(space6, other.space6, t)!,
      radiusSmall: BorderRadius.lerp(radiusSmall, other.radiusSmall, t)!,
      radiusMedium: BorderRadius.lerp(radiusMedium, other.radiusMedium, t)!,
      radiusLarge: BorderRadius.lerp(radiusLarge, other.radiusLarge, t)!,
    );
  }
}
