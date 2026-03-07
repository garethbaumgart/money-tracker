import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/shell/money_tracker_shell.dart';

void main() {
  testWidgets('renders app shell with compact navigation', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final expectedTitle = todayPlanTitle();

    await tester.pumpWidget(const MoneyTrackerApp(themeMode: ThemeMode.light));
    await tester.pumpAndSettle();

    expect(
      find.descendant(
        of: find.byType(AppBar),
        matching: find.text(expectedTitle),
      ),
      findsOneWidget,
    );
    expect(find.text('Shared dashboard'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.byType(NavigationRail), findsNothing);
  });

  group('todayPlanTitle', () {
    test('returns correct day name for each weekday', () {
      // 2026-03-09 is a Monday, 2026-03-15 is a Sunday
      const expected = [
        'Monday plan',
        'Tuesday plan',
        'Wednesday plan',
        'Thursday plan',
        'Friday plan',
        'Saturday plan',
        'Sunday plan',
      ];
      for (var i = 0; i < 7; i++) {
        final date = DateTime(2026, 3, 9 + i);
        expect(todayPlanTitle(date), expected[i]);
      }
    });
  });
}
