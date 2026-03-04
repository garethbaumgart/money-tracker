import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';

void main() {
  testWidgets('renders navigation rail on expanded width', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(1280, 900);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const MoneyTrackerApp(themeMode: ThemeMode.light));
    await tester.pumpAndSettle();

    expect(find.byType(NavigationRail), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
    expect(find.text('Priority checklist'), findsOneWidget);
  });

  testWidgets('applies dark theme semantic tokens and component themes', (
    WidgetTester tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const MoneyTrackerApp(themeMode: ThemeMode.dark));
    await tester.pumpAndSettle();

    final context = tester.element(find.byType(Scaffold).first);
    final tokens = AppThemeTokens.of(context);
    final theme = Theme.of(context);

    expect(tokens.stateDanger, const Color(0xFFFF8A80));
    expect(theme.filledButtonTheme.style, isNotNull);
    expect(theme.inputDecorationTheme.filled, isTrue);
    expect(theme.navigationBarTheme.indicatorColor, isNotNull);
  });
}
