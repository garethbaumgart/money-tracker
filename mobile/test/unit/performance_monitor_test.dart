import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/observability/performance_monitor.dart';

void main() {
  group('PerformanceMonitor', () {
    test('records time-to-first-frame', () {
      final monitor = PerformanceMonitor();

      expect(monitor.timeToFirstFrame, isNull);

      monitor.recordFirstFrame();

      expect(monitor.timeToFirstFrame, isNotNull);
      expect(monitor.timeToFirstFrame!.inMicroseconds, greaterThanOrEqualTo(0));
    });

    test('records time-to-interactive', () {
      final monitor = PerformanceMonitor();

      expect(monitor.timeToInteractive, isNull);

      monitor.recordInteractive();

      expect(monitor.timeToInteractive, isNotNull);
      expect(monitor.timeToInteractive!.inMicroseconds, greaterThanOrEqualTo(0));
    });

    test('first-frame is recorded only once', () {
      final monitor = PerformanceMonitor();

      monitor.recordFirstFrame();
      final firstValue = monitor.timeToFirstFrame;

      // Second call should not overwrite
      monitor.recordFirstFrame();

      expect(monitor.timeToFirstFrame, equals(firstValue));
    });

    test('interactive is recorded only once', () {
      final monitor = PerformanceMonitor();

      monitor.recordInteractive();
      final firstValue = monitor.timeToInteractive;

      // Second call should not overwrite
      monitor.recordInteractive();

      expect(monitor.timeToInteractive, equals(firstValue));
    });
  });
}
