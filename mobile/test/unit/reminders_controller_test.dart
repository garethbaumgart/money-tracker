import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/reminders/reminders_controller.dart';

void main() {
  test('reminders list is empty on construction', () {
    final controller = RemindersController();
    addTearDown(controller.dispose);

    expect(controller.reminders, isEmpty);
  });

  test('seedSample populates two sample reminders', () {
    final controller = RemindersController();
    addTearDown(controller.dispose);

    controller.seedSample();

    expect(controller.reminders, hasLength(2));
    expect(controller.reminders[0].title, 'Phone bill');
    expect(controller.reminders[0].amount, 82.0);
    expect(controller.reminders[1].title, 'Gym membership');
    expect(controller.reminders[1].amount, 35.0);
  });

  test('seedSample is idempotent when called twice', () {
    final controller = RemindersController();
    addTearDown(controller.dispose);

    controller.seedSample();
    controller.seedSample();

    expect(controller.reminders, hasLength(2));
  });

  test('addReminder inserts and maintains sort order by dueDate', () {
    final controller = RemindersController();
    addTearDown(controller.dispose);

    final now = DateTime.now();
    final laterReminder = ReminderEntry(
      id: 'r1',
      title: 'Later',
      amount: 10.0,
      dueDate: now.add(const Duration(days: 30)),
      cadence: 'Monthly',
    );
    final earlierReminder = ReminderEntry(
      id: 'r2',
      title: 'Earlier',
      amount: 20.0,
      dueDate: now.add(const Duration(days: 5)),
      cadence: 'Weekly',
    );

    controller.addReminder(laterReminder);
    controller.addReminder(earlierReminder);

    expect(controller.reminders, hasLength(2));
    expect(controller.reminders[0].title, 'Earlier');
    expect(controller.reminders[1].title, 'Later');
  });

  test('toggleNotifications updates flag and notifies listeners', () {
    final controller = RemindersController();
    addTearDown(controller.dispose);

    expect(controller.notificationsEnabled, isTrue);

    var notified = false;
    controller.addListener(() {
      notified = true;
    });

    controller.toggleNotifications(false);

    expect(controller.notificationsEnabled, isFalse);
    expect(notified, isTrue);
  });
}
