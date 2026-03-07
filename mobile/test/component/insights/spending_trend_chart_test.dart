import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';
import 'package:money_tracker/features/insights/domain/spending_analysis.dart';
import 'package:money_tracker/features/insights/presentation/widgets/spending_trend_chart.dart';

void main() {
  Widget buildTestWidget(
    List<CategorySpending> categories,
    String period, {
    Brightness brightness = Brightness.light,
  }) {
    final theme = ThemeData(brightness: brightness).copyWith(
      extensions: <ThemeExtension<dynamic>>[
        AppThemeTokens.fromBrightness(brightness),
      ],
    );

    return MaterialApp(
      theme: theme,
      home: Scaffold(
        body: SingleChildScrollView(
          child: SpendingTrendChart(
            categories: categories,
            period: period,
          ),
        ),
      ),
    );
  }

  group('SpendingTrendChart', () {
    testWidgets('displays category names', (tester) async {
      const categories = [
        CategorySpending(
          categoryId: 'cat-1',
          categoryName: 'Groceries',
          currentSpent: 500,
          previousSpent: 400,
          changePercent: 25,
        ),
        CategorySpending(
          categoryId: 'cat-2',
          categoryName: 'Dining',
          currentSpent: 200,
          previousSpent: 150,
          changePercent: 33.33,
        ),
      ];

      await tester.pumpWidget(buildTestWidget(categories, '30d'));

      expect(find.text('Groceries'), findsOneWidget);
      expect(find.text('Dining'), findsOneWidget);
    });

    testWidgets('shows empty message when no categories', (tester) async {
      await tester.pumpWidget(buildTestWidget(const [], '30d'));

      expect(find.text('No spending data for this period.'), findsOneWidget);
    });

    testWidgets('shows period in title', (tester) async {
      const categories = [
        CategorySpending(
          categoryId: 'cat-1',
          categoryName: 'Groceries',
          currentSpent: 500,
          previousSpent: 400,
          changePercent: 25,
        ),
      ];

      await tester.pumpWidget(buildTestWidget(categories, '7d'));

      expect(find.text('Spending by category (7d)'), findsOneWidget);
    });

    testWidgets('shows legend items', (tester) async {
      const categories = [
        CategorySpending(
          categoryId: 'cat-1',
          categoryName: 'Groceries',
          currentSpent: 500,
          previousSpent: 400,
          changePercent: 25,
        ),
      ];

      await tester.pumpWidget(buildTestWidget(categories, '30d'));

      expect(find.text('Current'), findsOneWidget);
      expect(find.text('Previous'), findsOneWidget);
    });
  });

  group('SpendingTrendChart change-percent colors', () {
    for (final brightness in Brightness.values) {
      final modeName = brightness == Brightness.light ? 'light' : 'dark';
      final tokens = AppThemeTokens.fromBrightness(brightness);

      testWidgets(
        'uses stateSuccess for negative change percent ($modeName mode)',
        (tester) async {
          const categories = [
            CategorySpending(
              categoryId: 'cat-1',
              categoryName: 'Groceries',
              currentSpent: 400,
              previousSpent: 500,
              changePercent: -20.0,
            ),
          ];

          await tester.pumpWidget(
            buildTestWidget(categories, '30d', brightness: brightness),
          );

          // Find the change-percent text widget
          final changeTextFinder = find.text('-20.0%');
          expect(changeTextFinder, findsOneWidget);

          final textWidget = tester.widget<Text>(changeTextFinder);
          expect(textWidget.style?.color, equals(tokens.stateSuccess));
        },
      );
    }
  });
}
