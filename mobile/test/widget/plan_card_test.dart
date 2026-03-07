import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/money_tracker_theme.dart';
import 'package:money_tracker/features/subscriptions/domain/offering.dart';
import 'package:money_tracker/features/subscriptions/presentation/plan_card.dart';

const _testAnnualPackage = Package(
  identifier: '\$rc_annual',
  packageType: PackageType.annual,
  productId: 'premium_annual',
  priceString: '\$49.99',
  priceAmountMicros: 49990000,
  currencyCode: 'USD',
  period: BillingPeriod.annual,
);

const _testMonthlyPackage = Package(
  identifier: '\$rc_monthly',
  packageType: PackageType.monthly,
  productId: 'premium_monthly',
  priceString: '\$5.99',
  priceAmountMicros: 5990000,
  currencyCode: 'USD',
  period: BillingPeriod.monthly,
);

const _testAnnualWithTrial = Package(
  identifier: '\$rc_annual',
  packageType: PackageType.annual,
  productId: 'premium_annual',
  priceString: '\$49.99',
  priceAmountMicros: 49990000,
  currencyCode: 'USD',
  period: BillingPeriod.annual,
  introPrice: IntroPrice(
    priceString: '\$0.00',
    priceAmountMicros: 0,
    periodDays: 14,
    cycles: 1,
  ),
);

Widget _buildTestWidget({
  required Package package,
  bool isSelected = false,
  VoidCallback? onTap,
  Package? monthlyPackage,
  bool isRecommended = false,
}) {
  return MaterialApp(
    theme: MoneyTrackerTheme.light(),
    home: Scaffold(
      body: PlanCard(
        package: package,
        isSelected: isSelected,
        onTap: onTap ?? () {},
        monthlyPackage: monthlyPackage,
        isRecommended: isRecommended,
      ),
    ),
  );
}

void main() {
  group('PlanCard', () {
    testWidgets('renders annual plan with correct title and price',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
      ));

      expect(find.text('Annual'), findsOneWidget);
      expect(find.text('\$49.99'), findsOneWidget);
      expect(find.text('per year'), findsOneWidget);
    });

    testWidgets('renders monthly plan with correct title and price',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testMonthlyPackage,
      ));

      expect(find.text('Monthly'), findsOneWidget);
      expect(find.text('\$5.99'), findsOneWidget);
      expect(find.text('per month'), findsOneWidget);
    });

    testWidgets('shows per-month equivalent for annual plan', (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
      ));

      // 49990000 / 12 / 1000000 = $4.17 (rounded)
      expect(find.textContaining('/month'), findsOneWidget);
    });

    testWidgets('does not show per-month equivalent for monthly plan',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testMonthlyPackage,
      ));

      expect(find.textContaining('/month'), findsNothing);
    });

    testWidgets('shows savings badge when recommended with monthly package',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
        monthlyPackage: _testMonthlyPackage,
        isRecommended: true,
      ));

      expect(find.text('Save 30%'), findsOneWidget);
    });

    testWidgets('does not show savings badge when not recommended',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
        monthlyPackage: _testMonthlyPackage,
        isRecommended: false,
      ));

      expect(find.textContaining('Save'), findsNothing);
    });

    testWidgets('shows selected indicator when selected', (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
        isSelected: true,
      ));

      expect(find.byIcon(Icons.radio_button_checked), findsOneWidget);
    });

    testWidgets('shows unselected indicator when not selected',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
        isSelected: false,
      ));

      expect(find.byIcon(Icons.radio_button_off), findsOneWidget);
    });

    testWidgets('calls onTap when tapped', (tester) async {
      bool tapped = false;

      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
        onTap: () => tapped = true,
      ));

      await tester.tap(find.byType(PlanCard));
      expect(tapped, true);
    });

    testWidgets('shows trial badge when intro price is free trial',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualWithTrial,
      ));

      expect(find.text('14-day free trial'), findsOneWidget);
    });

    testWidgets('does not show trial badge when no intro price',
        (tester) async {
      await tester.pumpWidget(_buildTestWidget(
        package: _testAnnualPackage,
      ));

      expect(find.textContaining('free trial'), findsNothing);
    });
  });
}
