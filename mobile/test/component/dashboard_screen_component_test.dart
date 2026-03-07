import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';
import 'package:money_tracker/features/dashboard/dashboard_controller.dart';
import 'package:money_tracker/features/dashboard/dashboard_screen.dart';

void main() {
  testWidgets('shows empty dashboard state when no data', (
    WidgetTester tester,
  ) async {
    final controller = DashboardController(
      initialState: DashboardState.empty(),
    );
    addTearDown(controller.dispose);

    final theme = ThemeData.light().copyWith(
      extensions: <ThemeExtension<dynamic>>[
        AppThemeTokens.fromBrightness(Brightness.light),
      ],
    );

    await tester.pumpWidget(
      MaterialApp(
        theme: theme,
        home: Scaffold(
          body: DashboardScreen(controller: controller),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('No dashboard data yet'), findsOneWidget);
  });
}
