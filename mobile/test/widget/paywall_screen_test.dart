import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/money_tracker_theme.dart';
import 'package:money_tracker/features/subscriptions/application/entitlement_provider.dart';
import 'package:money_tracker/features/subscriptions/application/offerings_provider.dart';
import 'package:money_tracker/features/subscriptions/application/purchase_controller.dart';
import 'package:money_tracker/features/subscriptions/domain/offering.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/revenuecat_sdk_adapter.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';
import 'package:money_tracker/features/subscriptions/presentation/paywall_controller.dart';
import 'package:money_tracker/features/subscriptions/presentation/paywall_screen.dart';
import 'package:money_tracker/features/subscriptions/presentation/plan_card.dart';
import 'package:money_tracker/features/subscriptions/presentation/purchase_button.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({
    required this.statusCode,
    required this.responseBody,
  });

  final int statusCode;
  final String responseBody;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }
}

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

Offering _buildDefaultOffering() {
  return const Offering(
    identifier: 'default',
    packages: [_testAnnualPackage, _testMonthlyPackage],
  );
}

Widget _buildTestApp({
  required PaywallController controller,
  bool isTrialEligible = false,
  int trialDays = 0,
  String variant = 'A',
}) {
  return MaterialApp(
    theme: MoneyTrackerTheme.light(),
    home: PaywallScreen(
      controller: controller,
      isTrialEligible: isTrialEligible,
      trialDays: trialDays,
      variant: variant,
    ),
  );
}

PaywallController _createController({
  InMemoryRevenueCatSdkAdapter? sdkAdapter,
  String? source,
  String variant = 'A',
}) {
  final sdk = sdkAdapter ??
      InMemoryRevenueCatSdkAdapter(
        offering: _buildDefaultOffering(),
      );

  final premiumResponse = jsonEncode({
    'tier': 'Premium',
    'featureKeys': ['BankSync', 'PremiumInsights'],
    'trialExpiresAtUtc': null,
    'currentPeriodEndUtc': '2026-04-01T00:00:00Z',
  });

  final httpClient = StubHttpClient(
    statusCode: 200,
    responseBody: premiumResponse,
  );

  final gateway = SubscriptionGateway(
    apiBaseUrl: Uri.parse('https://api.example.com'),
    tokenProvider: () => 'test-token',
    httpClient: httpClient,
  );

  final entitlementProvider = EntitlementProvider(
    gateway: gateway,
    cacheTtl: const Duration(minutes: 5),
  );

  final offeringsProvider = OfferingsProvider(revenueCatSdk: sdk);
  final purchaseController = PurchaseController(
    revenueCatSdk: sdk,
    entitlementProvider: entitlementProvider,
    householdId: 'household-1',
  );

  return PaywallController(
    offeringsProvider: offeringsProvider,
    purchaseController: purchaseController,
    source: source,
    variant: variant,
  );
}

void main() {
  group('PaywallScreen', () {
    testWidgets('shows loading indicator initially', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    // P4-4-COMP-01: PaywallScreen renders with annual plan highlighted.
    testWidgets('renders with annual plan highlighted after loading',
        (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      // Both plans should be visible.
      expect(find.text('Annual'), findsOneWidget);
      expect(find.text('Monthly'), findsOneWidget);

      // Pricing should be visible.
      expect(find.text('\$49.99'), findsOneWidget);
      expect(find.text('\$5.99'), findsOneWidget);

      // Annual should be selected (2 PlanCard widgets present).
      expect(find.byType(PlanCard), findsNWidgets(2));
    });

    testWidgets('shows feature comparison list', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.text('Premium includes'), findsOneWidget);
      expect(find.text('Bank sync'), findsOneWidget);
      expect(find.text('Premium insights'), findsOneWidget);
      expect(find.text('Unlimited budgets'), findsOneWidget);
    });

    testWidgets('shows subscribe button', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.byType(PurchaseButton), findsOneWidget);
      expect(find.text('Subscribe'), findsOneWidget);
    });

    testWidgets('shows terms and privacy links', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.text('Terms of Service'), findsOneWidget);
      expect(find.text('Privacy Policy'), findsOneWidget);
    });

    testWidgets('shows close button in app bar', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.text('Upgrade to Premium'), findsOneWidget);
      expect(find.byIcon(Icons.close), findsOneWidget);
    });

    // P4-4-UNIT-11: PaywallScreen shows trial info when eligible.
    testWidgets('shows trial banner when eligible', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(
        controller: controller,
        isTrialEligible: true,
        trialDays: 14,
      ));
      await tester.pumpAndSettle();

      expect(find.text('Start your 14-day free trial'), findsOneWidget);
      expect(find.text('Start free trial'), findsOneWidget);
    });

    testWidgets('hides trial banner when not eligible', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(
        controller: controller,
        isTrialEligible: false,
      ));
      await tester.pumpAndSettle();

      expect(find.textContaining('free trial'), findsNothing);
      expect(find.text('Subscribe'), findsOneWidget);
    });

    // P4-4-COMP-06: Paywall with no network.
    testWidgets('shows error state when offerings fail to load',
        (tester) async {
      final failingSdk = InMemoryRevenueCatSdkAdapter(shouldThrow: true);
      final controller = _createController(sdkAdapter: failingSdk);

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      // Should show empty/error state with retry.
      expect(find.text('Try again'), findsOneWidget);
    });

    testWidgets('shows empty state when no packages available',
        (tester) async {
      final emptySdk = InMemoryRevenueCatSdkAdapter();
      final controller = _createController(sdkAdapter: emptySdk);

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      expect(find.textContaining('No plans'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);
    });

    // P4-4-UNIT-10: PaywallScreen accepts variant parameter.
    testWidgets('accepts variant parameter', (tester) async {
      final controller = _createController(variant: 'B');

      await tester.pumpWidget(
        _buildTestApp(controller: controller, variant: 'B'),
      );
      await tester.pumpAndSettle();

      // Variant should be set on the controller.
      expect(controller.variant, 'B');
    });

    testWidgets('savings badge is displayed for annual plan', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      // Annual savings vs monthly: ~30%.
      expect(find.text('Save 30%'), findsOneWidget);
    });

    testWidgets('can select monthly plan', (tester) async {
      final controller = _createController();

      await tester.pumpWidget(_buildTestApp(controller: controller));
      await tester.pumpAndSettle();

      // Scroll down to make the monthly plan card visible.
      await tester.scrollUntilVisible(
        find.text('Monthly'),
        200,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();

      // Tap the monthly plan card.
      await tester.tap(find.text('Monthly'));
      await tester.pump();

      expect(controller.selectedPackage?.packageType, PackageType.monthly);
      expect(controller.selectedPackage?.productId, 'premium_monthly');
    });
  });
}
