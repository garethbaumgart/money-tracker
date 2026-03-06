import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/config/app_config.dart';
import 'package:money_tracker/app/observability/startup_error_reporter.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

typedef AppConfigLoader = AppConfig Function();
typedef ThemeModePreferencesGatewayLoader = FutureOr<ThemeModePreferencesGateway> Function();
typedef RunMoneyTrackerApp = FutureOr<void> Function(Widget app);

Future<void> runMoneyTrackerApp({
  required StartupErrorReporter errorReporter,
  AppConfigLoader appConfigLoader = AppConfig.fromEnvironment,
  ThemeModePreferencesGatewayLoader? preferencesGatewayLoader,
  RunMoneyTrackerApp? runAppCallback,
}) async {
  await runZonedGuarded(
    () async {
      WidgetsFlutterBinding.ensureInitialized();
      FlutterError.onError = (details) => _reportStartupException(
            errorReporter,
            details.exception,
            details.stack ?? StackTrace.current,
          );

      final appConfig = appConfigLoader();
      final gatewayLoader =
          preferencesGatewayLoader ?? SharedPreferencesThemeModePreferencesGateway.new;
      final preferencesGateway = await Future.value(gatewayLoader());

      final initialMode = await _loadInitialThemeMode(preferencesGateway);
      final themeController = AppThemeController(
        initialMode: initialMode,
        preferencesGateway: preferencesGateway,
      );

      final launcher = runAppCallback ?? ((Widget app) => Future.sync(() => runApp(app)));
      try {
        await launcher(MoneyTrackerApp(
          themeController: themeController,
          appConfig: appConfig,
        ));
      } catch (error, stackTrace) {
        _reportStartupException(errorReporter, error, stackTrace);
      }
    },
    (error, stackTrace) => _reportStartupException(errorReporter, error, stackTrace),
  );
}

Future<AppThemeMode> _loadInitialThemeMode(
  ThemeModePreferencesGateway preferencesGateway,
) async {
  try {
    return await preferencesGateway.load();
  } catch (_) {
    return AppThemeMode.system;
  }
}

void _reportStartupException(
  StartupErrorReporter errorReporter,
  Object error,
  StackTrace stackTrace,
) {
  try {
    errorReporter.reportStartupException(error, stackTrace);
  } catch (_) {
    // Reporter implementations must not crash startup.
  }
}
