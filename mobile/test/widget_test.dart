import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/app.dart';

String _expectedPlanTitle() {
  const dayNames = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];
  final day = DateTime.now().weekday; // 1=Monday, 7=Sunday
  return '${dayNames[day - 1]} plan';
}

void main() {
  testWidgets('renders app shell with compact navigation', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    final expectedTitle = _expectedPlanTitle();

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
}
