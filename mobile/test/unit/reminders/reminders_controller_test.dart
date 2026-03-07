import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/reminders/reminders_controller.dart';

void main() {
  group('ReminderEntry.isOverdue', () {
    test('returns false when dueDate is today at 00:00', () {
      final now = DateTime.now();
      final todayMidnight = DateTime(now.year, now.month, now.day);
      final entry = _createEntry(dueDate: todayMidnight);

      expect(entry.isOverdue, isFalse);
    });

    test('returns false when dueDate is today at 23:59', () {
      final now = DateTime.now();
      final todayLate = DateTime(now.year, now.month, now.day, 23, 59, 59);
      final entry = _createEntry(dueDate: todayLate);

      expect(entry.isOverdue, isFalse);
    });

    test('returns true when dueDate is yesterday', () {
      final now = DateTime.now();
      final yesterday = DateTime(now.year, now.month, now.day)
          .subtract(const Duration(days: 1));
      final entry = _createEntry(dueDate: yesterday);

      expect(entry.isOverdue, isTrue);
    });

    test('returns false when dueDate is tomorrow', () {
      final now = DateTime.now();
      final tomorrow =
          DateTime(now.year, now.month, now.day).add(const Duration(days: 1));
      final entry = _createEntry(dueDate: tomorrow);

      expect(entry.isOverdue, isFalse);
    });

    test('returns true when dueDate is 30 days ago', () {
      final now = DateTime.now();
      final pastDate = DateTime(now.year, now.month, now.day)
          .subtract(const Duration(days: 30));
      final entry = _createEntry(dueDate: pastDate);

      expect(entry.isOverdue, isTrue);
    });
  });

  group('ReminderEntry.isDueToday', () {
    test('returns true when dueDate is today at 00:00', () {
      final now = DateTime.now();
      final todayMidnight = DateTime(now.year, now.month, now.day);
      final entry = _createEntry(dueDate: todayMidnight);

      expect(entry.isDueToday, isTrue);
    });

    test('returns true when dueDate is today at 23:59', () {
      final now = DateTime.now();
      final todayLate = DateTime(now.year, now.month, now.day, 23, 59, 59);
      final entry = _createEntry(dueDate: todayLate);

      expect(entry.isDueToday, isTrue);
    });

    test('returns false when dueDate is yesterday', () {
      final now = DateTime.now();
      final yesterday = DateTime(now.year, now.month, now.day)
          .subtract(const Duration(days: 1));
      final entry = _createEntry(dueDate: yesterday);

      expect(entry.isDueToday, isFalse);
    });

    test('returns false when dueDate is tomorrow', () {
      final now = DateTime.now();
      final tomorrow =
          DateTime(now.year, now.month, now.day).add(const Duration(days: 1));
      final entry = _createEntry(dueDate: tomorrow);

      expect(entry.isDueToday, isFalse);
    });
  });
}

ReminderEntry _createEntry({required DateTime dueDate}) {
  return ReminderEntry(
    id: 'test-id',
    title: 'Test reminder',
    amount: 50.0,
    dueDate: dueDate,
    cadence: 'Monthly',
  );
}
