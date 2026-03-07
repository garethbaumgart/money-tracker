import 'analytics_service.dart';

/// Callback for loading the set of already-recorded milestones from storage.
typedef RecordedMilestonesLoader = Future<Set<String>> Function();

/// Callback for saving the set of recorded milestones to storage.
typedef RecordedMilestonesSaver = Future<void> Function(Set<String> milestones);

/// Wraps [AnalyticsService] with per-user deduplication so each activation
/// milestone is tracked at most once per user session (or across sessions
/// when backed by persistent storage).
class ActivationTracker {
  ActivationTracker({
    required AnalyticsService analyticsService,
    RecordedMilestonesLoader? loader,
    RecordedMilestonesSaver? saver,
  })  : _analyticsService = analyticsService,
        _loader = loader,
        _saver = saver;

  final AnalyticsService _analyticsService;
  final RecordedMilestonesLoader? _loader;
  final RecordedMilestonesSaver? _saver;
  final Set<String> _recorded = {};
  bool _restored = false;

  /// The set of milestones that have already been recorded.
  Set<String> get recorded => Set.unmodifiable(_recorded);

  /// Restores previously recorded milestones from persistent storage.
  Future<void> restore() async {
    if (_loader == null) return;
    try {
      final loaded = await _loader();
      _recorded.addAll(loaded);
      _restored = true;
    } catch (_) {
      // Restore failure is tolerated; deduplication may re-emit.
    }
  }

  /// Tracks a milestone if it has not already been recorded for this user.
  ///
  /// Returns `true` if the milestone was newly tracked, `false` if it was
  /// a duplicate.
  Future<bool> trackIfNew(
    String milestone, {
    String? householdId,
    Map<String, String>? metadata,
  }) async {
    if (!_restored && _loader != null) {
      await restore();
    }

    if (_recorded.contains(milestone)) {
      return false;
    }

    await _analyticsService.track(
      milestone,
      householdId: householdId,
      metadata: metadata,
    );
    _recorded.add(milestone);
    await _save();
    return true;
  }

  /// Resets the deduplication state (e.g. on user logout).
  Future<void> reset() async {
    _recorded.clear();
    _restored = false;
    await _save();
  }

  Future<void> _save() async {
    if (_saver == null) return;
    try {
      await _saver(Set.from(_recorded));
    } catch (_) {
      // Save failure is tolerated.
    }
  }
}
