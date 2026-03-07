import 'package:flutter/foundation.dart';

/// Report structure for an unhandled crash.
class CrashReport {
  const CrashReport({
    required this.error,
    required this.stackTrace,
    required this.appVersion,
    this.deviceInfo,
  });

  final Object error;
  final StackTrace stackTrace;
  final String appVersion;
  final String? deviceInfo;
}

/// Abstract interface for crash reporting.
///
/// Concrete implementations (e.g., Sentry) should implement this interface.
/// The abstraction keeps the core app free from third-party crash reporting
/// SDK dependencies and allows easy testing.
abstract interface class CrashReporter {
  /// Reports an unhandled exception with associated metadata.
  void reportCrash(CrashReport report);
}

/// Default [CrashReporter] that logs crashes to the Flutter error console.
///
/// Used during development and as a fallback when no external crash
/// reporting service is configured.
class ConsoleCrashReporter implements CrashReporter {
  const ConsoleCrashReporter();

  @override
  void reportCrash(CrashReport report) {
    FlutterError.dumpErrorToConsole(
      FlutterErrorDetails(
        exception: report.error,
        stack: report.stackTrace,
        library: 'crash_reporter',
        context: ErrorDescription(
          'Unhandled exception (appVersion=${report.appVersion})',
        ),
      ),
      forceReport: true,
    );
  }
}

/// A [CrashReporter] that silently discards all reports.
///
/// Useful for tests where crash output would be noisy.
class NoopCrashReporter implements CrashReporter {
  const NoopCrashReporter();

  @override
  void reportCrash(CrashReport report) {}
}

/// A [CrashReporter] that records all reported crashes in a list.
///
/// Useful for unit tests to verify that crashes are captured correctly.
class RecordingCrashReporter implements CrashReporter {
  final List<CrashReport> reports = <CrashReport>[];

  @override
  void reportCrash(CrashReport report) {
    reports.add(report);
  }
}
