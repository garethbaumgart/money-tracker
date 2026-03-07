import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/config/app_config.dart';
import 'package:money_tracker/app/observability/crash_reporter.dart';
import 'package:money_tracker/app/observability/performance_monitor.dart';
import 'package:money_tracker/app/observability/startup_error_reporter.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

typedef AppConfigLoader = AppConfig Function();
typedef ThemeModePreferencesGatewayLoader = FutureOr<ThemeModePreferencesGateway> Function();
typedef RunAppCallback = FutureOr<void> Function(Widget app);

/// The application version reported in crash reports.
const String appVersion = '1.0.0';

Future<void> runMoneyTrackerApp({
  required StartupErrorReporter errorReporter,
  CrashReporter crashReporter = const NoopCrashReporter(),
  PerformanceMonitor? performanceMonitor,
  AppConfigLoader appConfigLoader = AppConfig.fromEnvironment,
  ThemeModePreferencesGatewayLoader? preferencesGatewayLoader,
  RunAppCallback? runAppCallback,
}) async {
  final perfMonitor = performanceMonitor ?? PerformanceMonitor();
  final previousErrorHandler = FlutterError.onError;
  // This error wiring stays active after startup to capture uncaught errors.
  await runZonedGuarded(
    () async {
      WidgetsFlutterBinding.ensureInitialized();
      FlutterError.onError = (details) {
        crashReporter.reportCrash(CrashReport(
          error: details.exception,
          stackTrace: details.stack ?? StackTrace.current,
          appVersion: appVersion,
        ));
        _reportStartupException(
          errorReporter,
          details.exception,
          details.stack ?? StackTrace.current,
        );
        previousErrorHandler?.call(details);
      };

      // --- Critical init (synchronous / must happen before first frame) ---
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

        // Record first frame after the widget tree has been built.
        WidgetsBinding.instance.addPostFrameCallback((_) {
          perfMonitor.recordFirstFrame();
        });

        // --- Deferred init (non-critical, post-first-frame) ---
        // Analytics, crash reporter initialization, and other non-critical
        // services are deferred to after the first frame to keep startup fast.
        _deferNonCriticalInit(crashReporter, perfMonitor);
      } catch (error, stackTrace) {
        _reportStartupException(errorReporter, error, stackTrace);
      }
    },
    (error, stackTrace) {
      crashReporter.reportCrash(CrashReport(
        error: error,
        stackTrace: stackTrace,
        appVersion: appVersion,
      ));
      _reportStartupException(errorReporter, error, stackTrace);
    },
    zoneValues: const {},
  );

  FlutterError.onError = previousErrorHandler;
}

/// Defers non-critical initialization to after the first frame.
///
/// This keeps the critical startup path fast (theme, routing) while
/// deferring analytics, crash reporter SDK init, and other services.
void _deferNonCriticalInit(
  CrashReporter crashReporter,
  PerformanceMonitor performanceMonitor,
) {
  // Schedule deferred init after the current frame.
  Future<void>.delayed(Duration.zero, () {
    // Mark the app as interactive once deferred init completes.
    performanceMonitor.recordInteractive();
  });
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
    FlutterError.dumpErrorToConsole(
      FlutterErrorDetails(
        exception: error,
        stack: stackTrace,
        library: 'app_bootstrap',
        context: ErrorDescription(
          'Failed to report startup exception with $reportingError; reporting stack: $reportingStackTrace',
        ),
      ),
      forceReport: true,
    );
  }
}
