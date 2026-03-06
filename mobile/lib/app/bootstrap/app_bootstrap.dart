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
typedef RunAppCallback = FutureOr<void> Function(Widget app);

Future<void> runMoneyTrackerApp({
  required StartupErrorReporter errorReporter,
  AppConfigLoader appConfigLoader = AppConfig.fromEnvironment,
  ThemeModePreferencesGatewayLoader? preferencesGatewayLoader,
  RunAppCallback? runAppCallback,
}) async {
  final previousErrorHandler = FlutterError.onError;
  // This error wiring stays active after startup to capture uncaught errors.
  await runZonedGuarded(
    () async {
      WidgetsFlutterBinding.ensureInitialized();
      FlutterError.onError = (details) {
        _reportStartupException(
          errorReporter,
          details.exception,
          details.stack ?? StackTrace.current,
        );
        previousErrorHandler?.call(details);
      };

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
    zoneValues: const {},
  );

  FlutterError.onError = previousErrorHandler;
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
  } catch (reportingError, reportingStackTrace) {
    // Reporter implementations must not crash startup.
    FlutterError.reportError(
      FlutterErrorDetails(
        exception: error,
        stack: stackTrace,
        library: 'app_bootstrap',
        context: ErrorDescription(
          'Failed to report startup exception with $reportingError; reporting stack: $reportingStackTrace',
        ),
      ),
    );
  }
}
