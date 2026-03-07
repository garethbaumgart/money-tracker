import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_event.dart';
import 'package:money_tracker/shared_kernel/analytics/analytics_queue.dart';

void main() {
  group('AnalyticsQueue', () {
    AnalyticsEvent makeEvent(String milestone) => AnalyticsEvent(
          milestone: milestone,
          occurredAtUtc: DateTime.utc(2026, 3, 7, 12, 0, 0),
        );

    test('enqueue adds events and flushes at batch size', () async {
      final sent = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (batch) async {
          sent.add(List.of(batch));
          return true;
        },
        batchSize: 2,
      );

      await queue.enqueue(makeEvent('signup_completed'));
      expect(sent.length, 0); // Not yet at batch size.

      await queue.enqueue(makeEvent('household_created'));
      expect(sent.length, 1);
      expect(sent.first.length, 2);
      expect(sent.first[0].milestone, 'signup_completed');
      expect(sent.first[1].milestone, 'household_created');
    });

    test('flush sends all queued events', () async {
      final sent = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (batch) async {
          sent.add(List.of(batch));
          return true;
        },
        batchSize: 100, // Large batch size to prevent auto-flush.
      );

      await queue.enqueue(makeEvent('signup_completed'));
      await queue.enqueue(makeEvent('household_created'));
      expect(sent.length, 0);

      await queue.flush();
      expect(sent.length, 1);
      expect(sent.first.length, 2);
    });

    test('flush retains events on sender failure', () async {
      var failCount = 0;
      final sent = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (batch) async {
          failCount++;
          if (failCount <= 1) return false;
          sent.add(List.of(batch));
          return true;
        },
        batchSize: 100,
      );

      await queue.enqueue(makeEvent('signup_completed'));
      await queue.flush(); // First attempt fails.
      expect(queue.length, 1); // Event retained.
      expect(sent.length, 0);

      await queue.flush(); // Second attempt succeeds.
      expect(queue.length, 0);
      expect(sent.length, 1);
    });

    test('flush tolerates sender exceptions', () async {
      final queue = AnalyticsQueue(
        sender: (_) async => throw Exception('network error'),
        batchSize: 100,
      );

      await queue.enqueue(makeEvent('signup_completed'));
      await queue.flush(); // Should not throw.
      expect(queue.length, 1); // Event retained.
    });

    test('flush is a no-op when queue is empty', () async {
      var senderCalled = false;
      final queue = AnalyticsQueue(
        sender: (_) async {
          senderCalled = true;
          return true;
        },
        batchSize: 100,
      );

      await queue.flush();
      expect(senderCalled, false);
    });

    test('persister is called on enqueue and flush', () async {
      final persisted = <List<AnalyticsEvent>>[];
      final queue = AnalyticsQueue(
        sender: (_) async => true,
        persister: (events) async => persisted.add(List.of(events)),
        batchSize: 100,
      );

      await queue.enqueue(makeEvent('signup_completed'));
      expect(persisted.length, 1);
      expect(persisted.last.length, 1);

      await queue.flush();
      // After flush, persist is called with empty queue.
      expect(persisted.last.length, 0);
    });

    test('restore loads events from loader', () async {
      final queue = AnalyticsQueue(
        sender: (_) async => true,
        loader: () async => [makeEvent('signup_completed')],
        batchSize: 100,
      );

      await queue.restore();
      expect(queue.length, 1);
    });

    test('restore tolerates loader failure', () async {
      final queue = AnalyticsQueue(
        sender: (_) async => true,
        loader: () async => throw Exception('storage error'),
        batchSize: 100,
      );

      await queue.restore(); // Should not throw.
      expect(queue.length, 0);
    });

    test('serialize and deserialize round-trips events', () {
      final events = [
        AnalyticsEvent(
          milestone: 'signup_completed',
          householdId: 'hh-1',
          metadata: {'key': 'value'},
          occurredAtUtc: DateTime.utc(2026, 3, 7, 12, 0, 0),
        ),
        AnalyticsEvent(
          milestone: 'household_created',
          occurredAtUtc: DateTime.utc(2026, 3, 7, 13, 0, 0),
        ),
      ];

      final json = AnalyticsQueue.serialize(events);
      final restored = AnalyticsQueue.deserialize(json);

      expect(restored.length, 2);
      expect(restored[0].milestone, 'signup_completed');
      expect(restored[0].householdId, 'hh-1');
      expect(restored[0].metadata, {'key': 'value'});
      expect(restored[1].milestone, 'household_created');
      expect(restored[1].householdId, isNull);
    });
  });
}
