import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/app.dart';

void main() {
  Future<void> _pumpAppAndNavigateTo(
    WidgetTester tester,
    String destinationLabel,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const MoneyTrackerApp(themeMode: ThemeMode.light));
    await tester.pumpAndSettle();

    await tester.tap(find.text(destinationLabel));
    await tester.pumpAndSettle();
  }

  group('Explore roadmap button on stub screens', () {
    testWidgets('shows SnackBar when tapped on Budgets stub', (
      WidgetTester tester,
    ) async {
      await _pumpAppAndNavigateTo(tester, 'Budgets');

      await tester.tap(find.text('Explore roadmap'));
      await tester.pump();

      expect(find.text('Roadmap coming soon'), findsOneWidget);
    });

    testWidgets('shows SnackBar when tapped on Activity stub', (
      WidgetTester tester,
    ) async {
      await _pumpAppAndNavigateTo(tester, 'Activity');

      await tester.tap(find.text('Explore roadmap'));
      await tester.pump();

      expect(find.text('Roadmap coming soon'), findsOneWidget);
    });

    testWidgets('shows SnackBar when tapped on Household stub', (
      WidgetTester tester,
    ) async {
      await _pumpAppAndNavigateTo(tester, 'Household');

      await tester.tap(find.text('Explore roadmap'));
      await tester.pump();

      expect(find.text('Roadmap coming soon'), findsOneWidget);
    });

    testWidgets('SnackBar uses floating behavior', (
      WidgetTester tester,
    ) async {
      await _pumpAppAndNavigateTo(tester, 'Budgets');

      await tester.tap(find.text('Explore roadmap'));
      await tester.pump();

      final snackBar = tester.widget<SnackBar>(find.byType(SnackBar));
      expect(snackBar.behavior, SnackBarBehavior.floating);
    });
  });
}
