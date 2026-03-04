import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

void main() {
  test('setMode keeps applied mode when persistence fails', () async {
    final controller = AppThemeController(
      initialMode: AppThemeMode.system,
      preferencesGateway: _ThrowingThemeModePreferencesGateway(),
    );
    addTearDown(controller.dispose);

    await expectLater(controller.setMode(AppThemeMode.dark), completes);
    expect(controller.mode, AppThemeMode.dark);
  });
}

class _ThrowingThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  @override
  Future<AppThemeMode> load() async {
    return AppThemeMode.system;
  }

  @override
  Future<void> save(AppThemeMode mode) {
    throw Exception('simulated write failure');
  }
}
