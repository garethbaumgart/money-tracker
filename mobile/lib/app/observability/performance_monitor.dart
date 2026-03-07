/// Tracks application startup performance metrics.
///
/// Measures time-to-first-frame (TTFF) and time-to-interactive (TTI)
/// to enable monitoring of app launch performance.
class PerformanceMonitor {
  PerformanceMonitor() : _createdAt = DateTime.now();

  final DateTime _createdAt;
  Duration? _timeToFirstFrame;
  Duration? _timeToInteractive;

  /// The measured time from monitor creation to the first frame callback.
  Duration? get timeToFirstFrame => _timeToFirstFrame;

  /// The measured time from monitor creation to the app becoming interactive.
  Duration? get timeToInteractive => _timeToInteractive;

  /// Records the time-to-first-frame metric.
  ///
  /// Should be called from [WidgetsBinding.instance.addPostFrameCallback]
  /// after the first frame has been rendered.
  void recordFirstFrame() {
    _timeToFirstFrame ??= DateTime.now().difference(_createdAt);
  }

  /// Records the time-to-interactive metric.
  ///
  /// Should be called once all critical initialization is complete and
  /// the app is ready for user interaction.
  void recordInteractive() {
    _timeToInteractive ??= DateTime.now().difference(_createdAt);
  }
}
