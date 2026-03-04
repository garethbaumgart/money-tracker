import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';

void main() {
  group('AppThemeModeResolver', () {
    const resolver = AppThemeModeResolver();

    test('returns System when no stored value exists', () {
      expect(resolver.resolve(null), AppThemeMode.system);
      expect(resolver.resolve(''), AppThemeMode.system);
    });

    test('returns System for unknown values', () {
      expect(resolver.resolve('invalid'), AppThemeMode.system);
      expect(resolver.resolve(' high-contrast '), AppThemeMode.system);
    });

    test('resolves valid stored values', () {
      expect(resolver.resolve('system'), AppThemeMode.system);
      expect(resolver.resolve('light'), AppThemeMode.light);
      expect(resolver.resolve('dark'), AppThemeMode.dark);
      expect(resolver.resolve('LiGhT'), AppThemeMode.light);
    });
  });

  group('AppThemeMode material mapping', () {
    test('maps to ThemeMode and back without loss', () {
      for (final mode in AppThemeMode.values) {
        final materialMode = mode.toMaterialThemeMode();
        expect(AppThemeMode.fromMaterialThemeMode(materialMode), mode);
      }
    });
  });
}
