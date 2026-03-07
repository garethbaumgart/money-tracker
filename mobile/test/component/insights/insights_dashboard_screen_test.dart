import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';
import 'package:money_tracker/features/insights/application/insights_controller.dart';
import 'package:money_tracker/features/insights/presentation/insights_dashboard_screen.dart';

void main() {
  Widget buildTestWidget(InsightsController controller) {
    final theme = ThemeData.light().copyWith(
      extensions: <ThemeExtension<dynamic>>[
        AppThemeTokens.fromBrightness(Brightness.light),
      ],
    );

    return MaterialApp(
      theme: theme,
      home: Scaffold(
        body: InsightsDashboardScreen(controller: controller),
      ),
    );
  }

  group('InsightsDashboardScreen', () {
    testWidgets('shows loading indicator when loading', (tester) async {
      final controller = InsightsController(
        initialState: InsightsState(loadState: InsightsLoadState.loading),
      );
      addTearDown(controller.dispose);

      await tester.pumpWidget(buildTestWidget(controller));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('shows paywall gate for non-premium users', (tester) async {
      final controller = InsightsController(
        initialState:
            InsightsState(loadState: InsightsLoadState.premiumRequired),
      );
      addTearDown(controller.dispose);

      await tester.pumpWidget(buildTestWidget(controller));

      expect(find.text('Premium Insights'), findsOneWidget);
      expect(find.text('Upgrade to Premium'), findsOneWidget);
    });

    testWidgets('shows error state with message', (tester) async {
      final controller = InsightsController(
        initialState: InsightsState(
          loadState: InsightsLoadState.error,
          errorMessage: 'Something went wrong.',
        ),
      );
      addTearDown(controller.dispose);

      await tester.pumpWidget(buildTestWidget(controller));

      expect(find.text('Something went wrong.'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
    });

    testWidgets('shows insights content with sample data', (tester) async {
      final controller = InsightsController(
        initialState: InsightsState.sample(),
      );
      addTearDown(controller.dispose);

      await tester.pumpWidget(buildTestWidget(controller));

      // Period selector
      expect(find.text('7 days'), findsOneWidget);
      expect(find.text('30 days'), findsOneWidget);
      expect(find.text('90 days'), findsOneWidget);

      // Spending chart
      expect(find.text('Spending by category (30d)'), findsOneWidget);

      // Budget health
      expect(find.text('Budget health'), findsOneWidget);

      // Anomaly alerts
      expect(find.text('Spending alerts'), findsOneWidget);
    });

    testWidgets('shows idle state message when no data', (tester) async {
      final controller = InsightsController(
        initialState: InsightsState(loadState: InsightsLoadState.idle),
      );
      addTearDown(controller.dispose);

      await tester.pumpWidget(buildTestWidget(controller));

      expect(find.text('No insights data available.'), findsOneWidget);
    });
  });
}
