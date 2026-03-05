import 'package:flutter/material.dart';

import '../../shared_kernel/preferences/theme_mode_preferences_gateway.dart';
import 'app_theme_mode.dart';

class AppThemeController extends ChangeNotifier {
  AppThemeController({
    required AppThemeMode initialMode,
    required ThemeModePreferencesGateway preferencesGateway,
  }) : _mode = initialMode,
       _preferencesGateway = preferencesGateway;

  AppThemeMode _mode;
  final ThemeModePreferencesGateway _preferencesGateway;
  Future<void> _saveChain = Future<void>.value();

  AppThemeMode get mode => _mode;

  ThemeMode get materialThemeMode => _mode.toMaterialThemeMode();

  Future<void> setMode(AppThemeMode mode) async {
    if (_mode == mode) {
      return;
    }

    _mode = mode;
    notifyListeners();
    final modeToPersist = mode;
    _saveChain = _saveChain.then((_) async {
      try {
        await _preferencesGateway.save(modeToPersist);
      } catch (_) {
        // Keep the in-memory mode applied even if persistence is temporarily unavailable.
      }
    });
    await _saveChain;
  }
}

class AppThemeControllerScope extends InheritedNotifier<AppThemeController> {
  const AppThemeControllerScope({
    super.key,
    required AppThemeController controller,
    required super.child,
  }) : super(notifier: controller);

  static AppThemeController of(BuildContext context) {
    final scope = context
        .dependOnInheritedWidgetOfExactType<AppThemeControllerScope>();
    if (scope == null || scope.notifier == null) {
      throw StateError('AppThemeControllerScope is missing from the tree.');
    }

    return scope.notifier!;
  }
}
