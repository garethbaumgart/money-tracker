import 'dart:async';
import 'dart:collection';
import 'dart:convert';

import 'analytics_event.dart';

/// Callback for sending a batch of events to the analytics API.
typedef EventBatchSender = Future<bool> Function(List<AnalyticsEvent> batch);

/// Callback for persisting the queue to durable storage.
typedef QueuePersister = Future<void> Function(List<AnalyticsEvent> events);

/// Callback for loading persisted queue from durable storage.
typedef QueueLoader = Future<List<AnalyticsEvent>> Function();

/// A local queue that batches [AnalyticsEvent] items and sends them
/// asynchronously via [EventBatchSender]. Events are persisted to local
/// storage so they survive app restarts.
class AnalyticsQueue {
  AnalyticsQueue({
    required EventBatchSender sender,
    QueuePersister? persister,
    QueueLoader? loader,
    int batchSize = 10,
    Duration flushInterval = const Duration(seconds: 30),
  })  : _sender = sender,
        _persister = persister,
        _loader = loader,
        _batchSize = batchSize,
        _flushInterval = flushInterval;

  final EventBatchSender _sender;
  final QueuePersister? _persister;
  final QueueLoader? _loader;
  final int _batchSize;
  final Duration _flushInterval;

  final Queue<AnalyticsEvent> _queue = Queue<AnalyticsEvent>();
  Timer? _flushTimer;
  bool _isFlushing = false;

  /// The number of events currently queued.
  int get length => _queue.length;

  /// Loads any previously persisted events from local storage.
  Future<void> restore() async {
    if (_loader == null) return;
    try {
      final events = await _loader();
      _queue.addAll(events);
    } catch (_) {
      // Restore failure is tolerated; events may be lost.
    }
  }

  /// Starts the periodic flush timer.
  void start() {
    _flushTimer?.cancel();
    _flushTimer = Timer.periodic(_flushInterval, (_) => flush());
  }

  /// Stops the periodic flush timer.
  void stop() {
    _flushTimer?.cancel();
    _flushTimer = null;
  }

  /// Enqueues an event and triggers a flush if the batch size is reached.
  Future<void> enqueue(AnalyticsEvent event) async {
    _queue.add(event);
    await _persist();
    if (_queue.length >= _batchSize) {
      await flush();
    }
  }

  /// Sends all queued events in batches. Events that fail to send remain
  /// in the queue for the next flush cycle.
  Future<void> flush() async {
    if (_isFlushing || _queue.isEmpty) return;
    _isFlushing = true;

    try {
      while (_queue.isNotEmpty) {
        final batch = <AnalyticsEvent>[];
        final count =
            _queue.length < _batchSize ? _queue.length : _batchSize;
        for (var i = 0; i < count; i++) {
          batch.add(_queue.removeFirst());
        }

        try {
          final success = await _sender(batch);
          if (!success) {
            // Put failed events back at the front of the queue.
            for (final event in batch.reversed) {
              _queue.addFirst(event);
            }
            break; // Stop trying; the next flush cycle will retry.
          }
        } catch (_) {
          // Sender threw; put events back and stop retrying.
          for (final event in batch.reversed) {
            _queue.addFirst(event);
          }
          break;
        }
      }
    } catch (_) {
      // Flush failure is tolerated; events remain queued.
    } finally {
      _isFlushing = false;
      await _persist();
    }
  }

  Future<void> _persist() async {
    if (_persister == null) return;
    try {
      await _persister(_queue.toList());
    } catch (_) {
      // Persist failure is tolerated.
    }
  }

  /// Disposes the queue, stopping the flush timer.
  void dispose() {
    stop();
  }

  /// Serializes the current queue contents to a JSON string.
  static String serialize(List<AnalyticsEvent> events) {
    return jsonEncode(events.map((e) => e.toJson()).toList());
  }

  /// Deserializes a JSON string to a list of events.
  static List<AnalyticsEvent> deserialize(String json) {
    final list = jsonDecode(json) as List;
    return list
        .map((e) => AnalyticsEvent.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
