import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/insights/presentation/widgets/insights_paywall_gate.dart';

void main() {
  group('InsightsPaywallGate', () {
    testWidgets('renders feature previews and upgrade button', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: InsightsPaywallGate(onUpgrade: () {}),
          ),
        ),
      );

      expect(find.text('Premium Insights'), findsOneWidget);
      expect(find.text('Spending trends'), findsOneWidget);
      expect(find.text('Budget health score'), findsOneWidget);
      expect(find.text('Anomaly alerts'), findsOneWidget);
      expect(find.text('Upgrade to Premium'), findsOneWidget);
    });

    testWidgets('calls onUpgrade when upgrade button is tapped',
        (tester) async {
      var callCount = 0;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: InsightsPaywallGate(
              onUpgrade: () => callCount++,
            ),
          ),
        ),
      );

      await tester.tap(find.text('Upgrade to Premium'));
      await tester.pump();

      expect(callCount, 1);
    });
  });
}
