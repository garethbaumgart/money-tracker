import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/theme/app_theme_tokens.dart';

void main() {
  group('AppThemeTokens', () {
    test('copyWith overrides selected values only', () {
      final base = AppThemeTokens.fromColorScheme(const ColorScheme.light());

      final updated = base.copyWith(contentPrimary: Colors.pink, space4: 42);

      expect(updated.contentPrimary, Colors.pink);
      expect(updated.space4, 42);
      expect(updated.space3, base.space3);
      expect(updated.radiusMedium, base.radiusMedium);
    });

    test('lerp interpolates token values', () {
      final light = AppThemeTokens.fromColorScheme(const ColorScheme.light());
      final dark = AppThemeTokens.fromColorScheme(const ColorScheme.dark());

      final midpoint = light.lerp(dark, 0.5);

      expect(midpoint.space4, closeTo((light.space4 + dark.space4) / 2, 0.001));
      expect(midpoint.contentPrimary, isNot(light.contentPrimary));
      expect(midpoint.contentPrimary, isNot(dark.contentPrimary));
    });
  });
}
