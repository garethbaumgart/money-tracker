import 'package:flutter/foundation.dart';

class ReminderEntry {
  ReminderEntry({
    required this.id,
    required this.title,
    required this.amount,
    required this.dueDate,
    required this.cadence,
    this.lastNotifiedAt,
    this.dispatchAttemptCount = 0,
    this.lastDispatchErrorCode,
    this.lastDispatchErrorMessage,
  });

  final String id;
  final String title;
  final double amount;
  final DateTime dueDate;
  final String cadence;
  final DateTime? lastNotifiedAt;
  final int dispatchAttemptCount;
  final String? lastDispatchErrorCode;
  final String? lastDispatchErrorMessage;

  bool get isOverdue => dueDate.isBefore(DateTime.now());
}

class RemindersController extends ChangeNotifier {
  final List<ReminderEntry> _reminders = [];
  bool _notificationsEnabled = true;

  List<ReminderEntry> get reminders => List.unmodifiable(_reminders);

  bool get notificationsEnabled => _notificationsEnabled;

  void toggleNotifications(bool enabled) {
    _notificationsEnabled = enabled;
    notifyListeners();
  }

  void addReminder(ReminderEntry reminder) {
    _reminders.add(reminder);
    _sortReminders();
    notifyListeners();
  }

  void seedSample() {
    if (_reminders.isNotEmpty) {
      return;
    }

    final now = DateTime.now();
    _reminders.addAll([
      ReminderEntry(
        id: 'sample-1',
        title: 'Phone bill',
        amount: 82.0,
        dueDate: now.add(const Duration(days: 4)),
        cadence: 'Monthly',
      ),
      ReminderEntry(
        id: 'sample-2',
        title: 'Gym membership',
        amount: 35.0,
        dueDate: now.add(const Duration(days: 10)),
        cadence: 'Monthly',
      ),
    ]);
    _sortReminders();
  }

  void _sortReminders() {
    _reminders.sort((a, b) => a.dueDate.compareTo(b.dueDate));
  }
}
