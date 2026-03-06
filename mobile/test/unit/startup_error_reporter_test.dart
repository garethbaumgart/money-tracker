import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/bootstrap/app_bootstrap.dart';
import 'package:money_tracker/app/config/app_config.dart';
import 'package:money_tracker/app/observability/startup_error_reporter.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

void main() {
  group('runMoneyTrackerApp', () {
    test('reports startup exceptions from app bootstrap', () async {
      final reporter = _RecordingStartupErrorReporter();

      await runMoneyTrackerApp(
        errorReporter: reporter,
        appConfigLoader: () => AppConfig.fromRaw(
          appEnv: 'local',
          apiBaseUrl: 'https://api.local.money-tracker.test',
        ),
        preferencesGatewayLoader: () => const NoopThemeModePreferencesGateway(),
        runAppCallback: (_) => throw StateError('startup failure'),
      );

      expect(reporter.calls.length, 1);
      expect(reporter.calls.first.error, isA<StateError>());
    });

    test('survives reporter failures while reporting startup exceptions', () async {
      await runMoneyTrackerApp(
        errorReporter: _ThrowingStartupErrorReporter(),
        appConfigLoader: () => AppConfig.fromRaw(
          appEnv: 'local',
          apiBaseUrl: 'https://api.local.money-tracker.test',
        ),
        preferencesGatewayLoader: () => const NoopThemeModePreferencesGateway(),
        runAppCallback: (_) => throw StateError('startup failure'),
      );

      expect(true, isTrue);
    });
  });
}

class _RecordingStartupErrorReporter implements StartupErrorReporter {
  final List<_StartupErrorRecord> calls = <_StartupErrorRecord>[];

  @override
  void reportStartupException(Object error, StackTrace stackTrace) {
    calls.add((error: error, stackTrace: stackTrace));
  }
}

class _ThrowingStartupErrorReporter implements StartupErrorReporter {
  @override
  void reportStartupException(Object error, StackTrace stackTrace) {
    throw StateError('reporter failure');
  }
}

typedef _StartupErrorRecord = ({Object error, StackTrace stackTrace});
