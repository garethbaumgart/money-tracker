import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/feedback/application/nps_scheduler.dart';

void main() {
  group('NpsScheduler', () {
    late DateTime currentTime;
    late NpsScheduler scheduler;

    setUp(() {
      currentTime = DateTime(2026, 3, 1);
      scheduler = NpsScheduler(nowProvider: () => currentTime);
    });

    test('not eligible when first launch not recorded', () {
      expect(scheduler.isEligible(), isFalse);
    });

    test('not eligible within 7 days of first launch', () {
      scheduler.recordFirstLaunch(DateTime(2026, 2, 25));

      // Only 4 days have passed
      expect(scheduler.isEligible(), isFalse);
    });

    test('eligible after 7 days of first launch', () {
      scheduler.recordFirstLaunch(DateTime(2026, 2, 22));

      // 7 days have passed
      expect(scheduler.isEligible(), isTrue);
    });

    test('not eligible within 30 days of last prompt', () {
      scheduler.recordFirstLaunch(DateTime(2026, 1, 1));
      scheduler.recordLastPrompt(DateTime(2026, 2, 10));

      // Only 19 days since last prompt
      expect(scheduler.isEligible(), isFalse);
    });

    test('eligible after 30 days of last prompt', () {
      scheduler.recordFirstLaunch(DateTime(2026, 1, 1));
      scheduler.recordLastPrompt(DateTime(2026, 1, 30));

      // 30 days since last prompt
      expect(scheduler.isEligible(), isTrue);
    });

    test('eligible exactly on 7th day', () {
      scheduler.recordFirstLaunch(DateTime(2026, 2, 22));

      currentTime = DateTime(2026, 3, 1);
      expect(scheduler.isEligible(), isTrue);
    });

    test('eligible exactly on 30th day after prompt', () {
      scheduler.recordFirstLaunch(DateTime(2026, 1, 1));
      scheduler.recordLastPrompt(DateTime(2026, 1, 30));

      currentTime = DateTime(2026, 3, 1);
      expect(scheduler.isEligible(), isTrue);
    });
  });
}
