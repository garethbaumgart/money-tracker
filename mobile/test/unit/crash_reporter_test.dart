import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/observability/crash_reporter.dart';

void main() {
  group('CrashReporter', () {
    test('RecordingCrashReporter captures unhandled exception', () {
      final reporter = RecordingCrashReporter();
      final error = StateError('test crash');
      final stackTrace = StackTrace.current;

      reporter.reportCrash(CrashReport(
        error: error,
        stackTrace: stackTrace,
        appVersion: '1.0.0',
      ));

      expect(reporter.reports.length, 1);
      expect(reporter.reports.first.error, isA<StateError>());
    });

    test('CrashReport includes app version', () {
      final reporter = RecordingCrashReporter();
      final error = Exception('version test');
      final stackTrace = StackTrace.current;

      reporter.reportCrash(CrashReport(
        error: error,
        stackTrace: stackTrace,
        appVersion: '2.5.0',
        deviceInfo: 'iPhone 15',
      ));

      expect(reporter.reports.first.appVersion, '2.5.0');
      expect(reporter.reports.first.deviceInfo, 'iPhone 15');
    });

    test('NoopCrashReporter silently discards reports', () {
      const reporter = NoopCrashReporter();

      // Should not throw
      reporter.reportCrash(CrashReport(
        error: Exception('noop test'),
        stackTrace: StackTrace.current,
        appVersion: '1.0.0',
      ));
    });
  });
}
