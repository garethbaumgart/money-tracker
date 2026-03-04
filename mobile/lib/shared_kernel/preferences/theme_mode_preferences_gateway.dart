import 'package:shared_preferences/shared_preferences.dart';

import '../../app/theme/app_theme_mode.dart';

abstract interface class ThemeModePreferencesGateway {
  Future<AppThemeMode> load();

  Future<void> save(AppThemeMode mode);
}

final class SharedPreferencesThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  SharedPreferencesThemeModePreferencesGateway({
    AppThemeModeResolver resolver = const AppThemeModeResolver(),
  }) : _resolver = resolver;

  static const _themeModeKey = 'settings.theme_mode';

  final AppThemeModeResolver _resolver;

  @override
  Future<AppThemeMode> load() async {
    final preferences = await SharedPreferences.getInstance();
    return _resolver.resolve(preferences.getString(_themeModeKey));
  }

  @override
  Future<void> save(AppThemeMode mode) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(_themeModeKey, mode.storageValue);
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
