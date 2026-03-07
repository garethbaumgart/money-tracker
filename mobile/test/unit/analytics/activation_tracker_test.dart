import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/shared_kernel/analytics/activation_tracker.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_event.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_queue.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_service.dart';

void main() {
  group('ActivationTracker', () {
    QueuedAnalyticsService makeService(List<List<AnalyticsEvent>> sent) {
      final queue = AnalyticsQueue(
        sender: (batch) async {
          sent.add(List.of(batch));
          return true;
        },
        batchSize: 1,
      );
      return QueuedAnalyticsService(
        queue: queue,
        utcNow: () => DateTime.utc(2026, 3, 7, 12, 0, 0),
      );
    }

    test('trackIfNew tracks first occurrence and returns true', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
      );

      final result = await tracker.trackIfNew('signup_completed');

      expect(result, true);
      expect(sent.length, 1);
      expect(sent.first.first.milestone, 'signup_completed');
    });

    test('trackIfNew returns false for duplicate milestone', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
      );

      await tracker.trackIfNew('signup_completed');
      final result = await tracker.trackIfNew('signup_completed');

      expect(result, false);
      expect(sent.length, 1); // Only tracked once.
    });

    test('trackIfNew tracks different milestones independently', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
      );

      final r1 = await tracker.trackIfNew('signup_completed');
      final r2 = await tracker.trackIfNew('household_created');

      expect(r1, true);
      expect(r2, true);
      expect(sent.length, 2);
    });

    test('trackIfNew passes householdId and metadata through', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
      );

      await tracker.trackIfNew(
        'household_created',
        householdId: 'hh-1',
        metadata: {'source': 'onboarding'},
      );

      expect(sent.first.first.householdId, 'hh-1');
      expect(sent.first.first.metadata, {'source': 'onboarding'});
    });

    test('restore loads previously recorded milestones', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
        loader: () async => {'signup_completed'},
      );

      await tracker.restore();

      // Should be deduplicated because restore loaded it.
      final result = await tracker.trackIfNew('signup_completed');
      expect(result, false);
      expect(sent.length, 0);
    });

    test('restore tolerates loader failure', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
        loader: () async => throw Exception('storage error'),
      );

      await tracker.restore(); // Should not throw.

      // Since restore failed, milestone is not in the recorded set.
      final result = await tracker.trackIfNew('signup_completed');
      expect(result, true);
    });

    test('saver is called when milestone is newly tracked', () async {
      final saved = <Set<String>>[];
      final tracker = ActivationTracker(
        analyticsService: const NoopAnalyticsService(),
        saver: (milestones) async => saved.add(Set.from(milestones)),
      );

      await tracker.trackIfNew('signup_completed');

      expect(saved.length, 1);
      expect(saved.first, {'signup_completed'});
    });

    test('reset clears recorded milestones', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
      );

      await tracker.trackIfNew('signup_completed');
      expect(tracker.recorded, {'signup_completed'});

      await tracker.reset();
      expect(tracker.recorded, isEmpty);

      // Can track again after reset.
      final result = await tracker.trackIfNew('signup_completed');
      expect(result, true);
      expect(sent.length, 2);
    });

    test('auto-restores on first trackIfNew if loader is set', () async {
      final sent = <List<AnalyticsEvent>>[];
      final tracker = ActivationTracker(
        analyticsService: makeService(sent),
        loader: () async => {'signup_completed'},
      );

      // Don't call restore() — trackIfNew should auto-restore.
      final result = await tracker.trackIfNew('signup_completed');
      expect(result, false); // Already recorded via auto-restore.
    });
  });
}
