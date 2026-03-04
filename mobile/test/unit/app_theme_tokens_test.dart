import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/theme/app_theme_tokens.dart';

void main() {
  group('AppThemeTokens', () {
    test('copyWith overrides selected values only', () {
      final base = AppThemeTokens.fromBrightness(Brightness.light);

      final updated = base.copyWith(contentPrimary: Colors.pink, space4: 42);

      expect(updated.contentPrimary, Colors.pink);
      expect(updated.space4, 42);
      expect(updated.space3, base.space3);
      expect(updated.radiusMedium, base.radiusMedium);
    });

    test('lerp interpolates token values', () {
      final light = AppThemeTokens.fromBrightness(Brightness.light);
      final dark = AppThemeTokens.fromBrightness(Brightness.dark);

      final midpoint = light.lerp(dark, 0.5);
      final atStart = light.lerp(dark, 0.0);
      final atEnd = light.lerp(dark, 1.0);

      expect(midpoint.space4, closeTo((light.space4 + dark.space4) / 2, 0.001));
      expect(midpoint.contentPrimary, isNot(light.contentPrimary));
      expect(midpoint.contentPrimary, isNot(dark.contentPrimary));
      expect(atStart.space4, closeTo(light.space4, 0.001));
      expect(atStart.contentPrimary, equals(light.contentPrimary));
      expect(atEnd.space4, closeTo(dark.space4, 0.001));
      expect(atEnd.contentPrimary, equals(dark.contentPrimary));
    });
  });
}
