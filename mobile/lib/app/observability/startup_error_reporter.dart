import 'package:flutter/foundation.dart';

abstract interface class StartupErrorReporter {
  void reportStartupException(Object error, StackTrace stackTrace);
}

class FlutterErrorStartupErrorReporter implements StartupErrorReporter {
  const FlutterErrorStartupErrorReporter();

  @override
  void reportStartupException(Object error, StackTrace stackTrace) {
    FlutterError.dumpErrorToConsole(
      FlutterErrorDetails(
        exception: error,
        stack: stackTrace,
        library: 'startup_error_reporter',
        context: ErrorDescription('Unhandled startup exception'),
      ),
      forceReport: true,
    );
  }
}

class NoopStartupErrorReporter implements StartupErrorReporter {
  const NoopStartupErrorReporter();

  @override
  void reportStartupException(Object error, StackTrace stackTrace) {}
}
