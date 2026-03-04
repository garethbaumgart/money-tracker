import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  group('SharedPreferencesThemeModePreferencesGateway', () {
    setUp(() {
      SharedPreferences.setMockInitialValues(<String, Object>{});
    });

    test('load resolves stored value', () async {
      SharedPreferences.setMockInitialValues(<String, Object>{
        'settings.theme_mode': 'dark',
      });
      final gateway = SharedPreferencesThemeModePreferencesGateway();

      final loadedMode = await gateway.load();

      expect(loadedMode, AppThemeMode.dark);
    });

    test('load falls back to system when preferences access fails', () async {
      final gateway = SharedPreferencesThemeModePreferencesGateway(
        preferencesProvider: () async {
          throw Exception('simulated preferences load failure');
        },
      );

      final loadedMode = await gateway.load();

      expect(loadedMode, AppThemeMode.system);
    });

    test('save persists selected mode value', () async {
      final gateway = SharedPreferencesThemeModePreferencesGateway();

      await gateway.save(AppThemeMode.light);
      final preferences = await SharedPreferences.getInstance();

      expect(preferences.getString('settings.theme_mode'), 'light');
    });

    test('save completes when preferences access fails', () async {
      final gateway = SharedPreferencesThemeModePreferencesGateway(
        preferencesProvider: () async {
          throw Exception('simulated preferences save failure');
        },
      );

      await expectLater(gateway.save(AppThemeMode.light), completes);
    });
  });
}
