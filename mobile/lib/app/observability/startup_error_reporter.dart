abstract interface class StartupErrorReporter {
  void reportStartupException(Object error, StackTrace stackTrace);
}

class NoopStartupErrorReporter implements StartupErrorReporter {
  const NoopStartupErrorReporter();

  @override
  void reportStartupException(Object error, StackTrace stackTrace) {}
}
