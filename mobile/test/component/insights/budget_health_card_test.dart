import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/insights/domain/budget_health.dart';
import 'package:money_tracker/features/insights/presentation/widgets/budget_health_card.dart';

void main() {
  Widget buildTestWidget(BudgetHealth health) {
    return MaterialApp(
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
}
