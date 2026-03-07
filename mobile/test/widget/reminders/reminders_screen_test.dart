import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/money_tracker_theme.dart';
import 'package:money_tracker/features/reminders/reminders_controller.dart';
import 'package:money_tracker/features/reminders/reminders_screen.dart';

Widget _buildTestApp({required RemindersController controller}) {
  return MaterialApp(
    theme: MoneyTrackerTheme.light(),
    home: RemindersScreen(controller: controller),
  );
}

void main() {
  group('RemindersScreen badge states', () {
    testWidgets('shows red "Overdue" badge for past-due reminder',
        (tester) async {
      final controller = RemindersController();
      final now = DateTime.now();
      final yesterday =
          DateTime(now.year, now.month, now.day).subtract(const Duration(days: 2));

      controller.addReminder(ReminderEntry(
        id: 'overdue-1',
        title: 'Overdue bill',
        amount: 100.0,
        dueDate: yesterday,
        cadence: 'Monthly',
      ));

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.text('Overdue'), findsOneWidget);
    });

    testWidgets('shows amber "Due today" badge for today reminder',
        (tester) async {
      final controller = RemindersController();
      final now = DateTime.now();
      final today = DateTime(now.year, now.month, now.day);

      controller.addReminder(ReminderEntry(
        id: 'today-1',
        title: 'Today bill',
        amount: 50.0,
        dueDate: today,
        cadence: 'Monthly',
      ));

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.text('Due today'), findsOneWidget);
    });

    testWidgets('shows green "Due <date>" badge for future reminder',
        (tester) async {
      final controller = RemindersController();
      final now = DateTime.now();
      final future =
          DateTime(now.year, now.month, now.day).add(const Duration(days: 7));

      controller.addReminder(ReminderEntry(
        id: 'future-1',
        title: 'Future bill',
        amount: 75.0,
        dueDate: future,
        cadence: 'Monthly',
      ));

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      // The badge should contain "Due " prefix (not "Overdue" or "Due today").
      expect(find.text('Overdue'), findsNothing);
      expect(find.text('Due today'), findsNothing);
      expect(find.textContaining('Due '), findsOneWidget);
    });
  });
}
