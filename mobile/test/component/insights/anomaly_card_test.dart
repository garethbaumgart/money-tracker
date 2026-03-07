import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/insights/domain/spending_analysis.dart';
import 'package:money_tracker/features/insights/presentation/widgets/anomaly_card.dart';

void main() {
  Widget buildTestWidget(SpendingAnomaly anomaly) {
    return MaterialApp(
      home: Scaffold(
        body: AnomalyCard(anomaly: anomaly),
      ),
    );
  }

  group('AnomalyCard', () {
    testWidgets('displays category name and amounts', (tester) async {
      const anomaly = SpendingAnomaly(
        categoryId: 'cat-1',
        categoryName: 'Dining',
        currentSpent: 450,
        previousSpent: 280,
        changePercent: 60.71,
      );

      await tester.pumpWidget(buildTestWidget(anomaly));

      expect(find.text('Dining'), findsOneWidget);
      expect(find.text('\$450 vs \$280 last period'), findsOneWidget);
      expect(find.text('+61%'), findsOneWidget);
    });

    testWidgets('shows trending up icon', (tester) async {
      const anomaly = SpendingAnomaly(
        categoryId: 'cat-1',
        categoryName: 'Transport',
        currentSpent: 900,
        previousSpent: 400,
        changePercent: 125.0,
      );

      await tester.pumpWidget(buildTestWidget(anomaly));

      expect(find.byIcon(Icons.trending_up), findsOneWidget);
    });
  });
}
