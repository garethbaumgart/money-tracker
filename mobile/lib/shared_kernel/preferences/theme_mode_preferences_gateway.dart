import 'package:shared_preferences/shared_preferences.dart';

import '../../app/theme/app_theme_mode.dart';

typedef SharedPreferencesProvider = Future<SharedPreferences> Function();

abstract interface class ThemeModePreferencesGateway {
  Future<AppThemeMode> load();

  Future<void> save(AppThemeMode mode);
}

final class SharedPreferencesThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  SharedPreferencesThemeModePreferencesGateway({
    AppThemeModeResolver resolver = const AppThemeModeResolver(),
    SharedPreferencesProvider preferencesProvider =
        SharedPreferences.getInstance,
  }) : _resolver = resolver,
       _preferencesProvider = preferencesProvider;

  static const _themeModeKey = 'settings.theme_mode';

  final AppThemeModeResolver _resolver;
  final SharedPreferencesProvider _preferencesProvider;

  @override
  Future<AppThemeMode> load() async {
    try {
      final preferences = await _preferencesProvider();
      return _resolver.resolve(preferences.getString(_themeModeKey));
    } catch (_) {
      return AppThemeMode.system;
    }
  }

  @override
  Future<void> save(AppThemeMode mode) async {
    try {
      final preferences = await _preferencesProvider();
      await preferences.setString(_themeModeKey, mode.storageValue);
    } catch (_) {
      // Persist failure is tolerated because mode remains applied in memory.
    }
  }
}

final class NoopThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  const NoopThemeModePreferencesGateway();

  @override
  Future<AppThemeMode> load() async {
    return AppThemeMode.system;
  }

  @override
  Future<void> save(AppThemeMode mode) async {}
}
