import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_event.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_queue.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_service.dart';

void main() {
  group('QueuedAnalyticsService', () {
    test('track enqueues an event with correct milestone', () async {
      final sent = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (batch) async {
          sent.add(batch);
          return true;
        },
        batchSize: 1,
      );

      final fixedTime = DateTime.utc(2026, 3, 7, 12, 0, 0);
      final service = QueuedAnalyticsService(
        queue: queue,
        utcNow: () => fixedTime,
      );

      await service.track('signup_completed');

      expect(sent.length, 1);
      expect(sent.first.length, 1);
      expect(sent.first.first.milestone, 'signup_completed');
      expect(sent.first.first.occurredAtUtc, fixedTime);
    });

    test('track includes householdId and metadata', () async {
      final sent = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (batch) async {
          sent.add(batch);
          return true;
        },
        batchSize: 1,
      );

      final fixedTime = DateTime.utc(2026, 3, 7, 12, 0, 0);
      final service = QueuedAnalyticsService(
        queue: queue,
        utcNow: () => fixedTime,
      );

      await service.track(
        'household_created',
        householdId: 'abc-123',
        metadata: {'source': 'onboarding'},
      );

      expect(sent.length, 1);
      final event = sent.first.first;
      expect(event.milestone, 'household_created');
      expect(event.householdId, 'abc-123');
      expect(event.metadata, {'source': 'onboarding'});
    });

    test('track swallows queue errors', () async {
      final queue = AnalyticsQueue(
        sender: (_) async => throw Exception('network error'),
        batchSize: 1,
      );

      final service = QueuedAnalyticsService(queue: queue);

      // Should not throw.
      await service.track('signup_completed');
    });
  });

  group('NoopAnalyticsService', () {
    test('track completes without error', () async {
      const service = NoopAnalyticsService();
      await expectLater(service.track('signup_completed'), completes);
    });
  });
}
