import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';
import 'package:money_tracker/features/insights/domain/budget_health.dart';
import 'package:money_tracker/features/insights/presentation/widgets/budget_health_card.dart';

void main() {
  Widget buildTestWidget(
    BudgetHealth health, {
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
          child: BudgetHealthCard(health: health),
        ),
      ),
    );
  }

  group('BudgetHealthCard', () {
    testWidgets('displays overall score', (tester) async {
      final health = BudgetHealth(
        householdId: 'test',
        periodStartUtc: '2026-03-01T00:00:00Z',
        periodEndUtc: '2026-04-01T00:00:00Z',
        overallScore: 77,
        scoreBreakdown: const ScoreBreakdown(
          adherenceScore: 90,
          adherenceWeight: 0.40,
          velocityScore: 80,
          velocityWeight: 0.35,
          billPaymentScore: 55,
          billPaymentWeight: 0.25,
        ),
        categoryHealth: const [],
      );

      await tester.pumpWidget(buildTestWidget(health));

      expect(find.text('77'), findsOneWidget);
      expect(find.text('out of 100'), findsOneWidget);
      expect(find.text('Budget health'), findsOneWidget);
    });

    testWidgets('displays score breakdown rows', (tester) async {
      final health = BudgetHealth(
        householdId: 'test',
        periodStartUtc: '2026-03-01T00:00:00Z',
        periodEndUtc: '2026-04-01T00:00:00Z',
        overallScore: 72,
        scoreBreakdown: const ScoreBreakdown(
          adherenceScore: 75,
          adherenceWeight: 0.40,
          velocityScore: 65,
          velocityWeight: 0.35,
          billPaymentScore: 80,
          billPaymentWeight: 0.25,
        ),
        categoryHealth: const [],
      );

      await tester.pumpWidget(buildTestWidget(health));

      expect(find.text('Adherence (40%)'), findsOneWidget);
      expect(find.text('Velocity (35%)'), findsOneWidget);
      expect(find.text('Bill payment (25%)'), findsOneWidget);
    });

    testWidgets('displays category health when present', (tester) async {
      final health = BudgetHealth(
        householdId: 'test',
        periodStartUtc: '2026-03-01T00:00:00Z',
        periodEndUtc: '2026-04-01T00:00:00Z',
        overallScore: 72,
        scoreBreakdown: const ScoreBreakdown(
          adherenceScore: 75,
          adherenceWeight: 0.40,
          velocityScore: 65,
          velocityWeight: 0.35,
          billPaymentScore: 80,
          billPaymentWeight: 0.25,
        ),
        categoryHealth: const [
          CategoryHealth(
            categoryId: 'cat-1',
            categoryName: 'Groceries',
            allocated: 900,
            spent: 700,
            status: 'OnTrack',
          ),
          CategoryHealth(
            categoryId: 'cat-2',
            categoryName: 'Dining',
            allocated: 400,
            spent: 450,
            status: 'OverBudget',
          ),
        ],
      );

      await tester.pumpWidget(buildTestWidget(health));

      expect(find.text('Category status'), findsOneWidget);
      expect(find.text('Groceries'), findsOneWidget);
      expect(find.text('On track'), findsOneWidget);
      expect(find.text('Dining'), findsOneWidget);
      expect(find.text('Over budget'), findsOneWidget);
    });
  });

  group('BudgetHealthCard score colors', () {
    for (final brightness in Brightness.values) {
      final modeName = brightness == Brightness.light ? 'light' : 'dark';
      final tokens = AppThemeTokens.fromBrightness(brightness);

      testWidgets(
        'uses stateSuccess color for score >= 80 ($modeName mode)',
        (tester) async {
          final health = BudgetHealth(
            householdId: 'test',
            periodStartUtc: '2026-03-01T00:00:00Z',
            periodEndUtc: '2026-04-01T00:00:00Z',
            overallScore: 95,
            scoreBreakdown: const ScoreBreakdown(
              adherenceScore: 90,
              adherenceWeight: 0.40,
              velocityScore: 95,
              velocityWeight: 0.35,
              billPaymentScore: 100,
              billPaymentWeight: 0.25,
            ),
            categoryHealth: const [],
          );

          await tester
              .pumpWidget(buildTestWidget(health, brightness: brightness));

          final indicator = tester.widget<CircularProgressIndicator>(
            find.byType(CircularProgressIndicator),
          );
          final animation =
              indicator.valueColor! as AlwaysStoppedAnimation<Color>;
          expect(animation.value, equals(tokens.stateSuccess));
        },
      );

      testWidgets(
        'uses stateWarning color for score >= 50 and < 80 ($modeName mode)',
        (tester) async {
          final health = BudgetHealth(
            householdId: 'test',
            periodStartUtc: '2026-03-01T00:00:00Z',
            periodEndUtc: '2026-04-01T00:00:00Z',
            overallScore: 65,
            scoreBreakdown: const ScoreBreakdown(
              adherenceScore: 60,
              adherenceWeight: 0.40,
              velocityScore: 70,
              velocityWeight: 0.35,
              billPaymentScore: 65,
              billPaymentWeight: 0.25,
            ),
            categoryHealth: const [],
          );

          await tester
              .pumpWidget(buildTestWidget(health, brightness: brightness));

          final indicator = tester.widget<CircularProgressIndicator>(
            find.byType(CircularProgressIndicator),
          );
          final animation =
              indicator.valueColor! as AlwaysStoppedAnimation<Color>;
          expect(animation.value, equals(tokens.stateWarning));
        },
      );

      testWidgets(
        'uses stateDanger color for score < 50 ($modeName mode)',
        (tester) async {
          final health = BudgetHealth(
            householdId: 'test',
            periodStartUtc: '2026-03-01T00:00:00Z',
            periodEndUtc: '2026-04-01T00:00:00Z',
            overallScore: 30,
            scoreBreakdown: const ScoreBreakdown(
              adherenceScore: 30,
              adherenceWeight: 0.40,
              velocityScore: 25,
              velocityWeight: 0.35,
              billPaymentScore: 35,
              billPaymentWeight: 0.25,
            ),
            categoryHealth: const [],
          );

          await tester
              .pumpWidget(buildTestWidget(health, brightness: brightness));

          final indicator = tester.widget<CircularProgressIndicator>(
            find.byType(CircularProgressIndicator),
          );
          final animation =
              indicator.valueColor! as AlwaysStoppedAnimation<Color>;
          expect(animation.value, equals(tokens.stateDanger));
        },
      );
    }
  });

  group('BudgetHealthCard category status colors', () {
    for (final brightness in Brightness.values) {
      final modeName = brightness == Brightness.light ? 'light' : 'dark';
      final tokens = AppThemeTokens.fromBrightness(brightness);

      testWidgets(
        'uses correct token colors for each category status ($modeName mode)',
        (tester) async {
          final health = BudgetHealth(
            householdId: 'test',
            periodStartUtc: '2026-03-01T00:00:00Z',
            periodEndUtc: '2026-04-01T00:00:00Z',
            overallScore: 72,
            scoreBreakdown: const ScoreBreakdown(
              adherenceScore: 75,
              adherenceWeight: 0.40,
              velocityScore: 65,
              velocityWeight: 0.35,
              billPaymentScore: 80,
              billPaymentWeight: 0.25,
            ),
            categoryHealth: const [
              CategoryHealth(
                categoryId: 'cat-1',
                categoryName: 'Groceries',
                allocated: 900,
                spent: 700,
                status: 'OnTrack',
              ),
              CategoryHealth(
                categoryId: 'cat-2',
                categoryName: 'Entertainment',
                allocated: 300,
                spent: 280,
                status: 'AtRisk',
              ),
              CategoryHealth(
                categoryId: 'cat-3',
                categoryName: 'Dining',
                allocated: 400,
                spent: 450,
                status: 'OverBudget',
              ),
            ],
          );

          await tester
              .pumpWidget(buildTestWidget(health, brightness: brightness));

          // Find all status dot containers (8x8 circles)
          final dotFinder = find.byWidgetPredicate(
            (widget) =>
                widget is Container &&
                widget.constraints?.maxWidth == 8 &&
                widget.constraints?.maxHeight == 8,
          );
          final dots = tester.widgetList<Container>(dotFinder).toList();
          expect(dots.length, equals(3));

          final dotColors = dots.map((dot) {
            final decoration = dot.decoration! as BoxDecoration;
            return decoration.color;
          }).toList();

          expect(dotColors[0], equals(tokens.stateSuccess));
          expect(dotColors[1], equals(tokens.stateWarning));
          expect(dotColors[2], equals(tokens.stateDanger));
        },
      );
    }
  });
}
