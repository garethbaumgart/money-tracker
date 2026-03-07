import 'analytics_event.dart';
import 'analytics_queue.dart';

/// Callback that provides the current UTC time. Useful for testing.
typedef UtcNowProvider = DateTime Function();

/// Singleton event tracking service for analytics milestones.
///
/// Wraps an [AnalyticsQueue] and provides a simple `track()` API.
/// All events are enqueued locally and sent asynchronously via the queue's
/// flush mechanism (fire-and-forget).
abstract interface class AnalyticsService {
  /// Tracks a milestone event.
  ///
  /// [milestone] must be a valid activation milestone name (e.g.
  /// "signup_completed", "household_created").
  Future<void> track(
    String milestone, {
    String? householdId,
    Map<String, String>? metadata,
  });
}

/// Default implementation that delegates to an [AnalyticsQueue].
class QueuedAnalyticsService implements AnalyticsService {
  QueuedAnalyticsService({
    required AnalyticsQueue queue,
    UtcNowProvider utcNow = _defaultUtcNow,
  })  : _queue = queue,
        _utcNow = utcNow;

  final AnalyticsQueue _queue;
  final UtcNowProvider _utcNow;

  static DateTime _defaultUtcNow() => DateTime.now().toUtc();

  @override
  Future<void> track(
    String milestone, {
    String? householdId,
    Map<String, String>? metadata,
  }) async {
    final event = AnalyticsEvent(
      milestone: milestone,
      householdId: householdId,
      metadata: metadata,
      occurredAtUtc: _utcNow(),
    );
    try {
      await _queue.enqueue(event);
    } catch (_) {
      // Fire-and-forget: tracking failures must not disrupt the app.
    }
  }
}

/// A no-op implementation for use in tests or when analytics is disabled.
class NoopAnalyticsService implements AnalyticsService {
  const NoopAnalyticsService();

  @override
  Future<void> track(
    String milestone, {
    String? householdId,
    Map<String, String>? metadata,
  }) async {}
}
