import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/subscriptions/application/entitlement_provider.dart';
import 'package:money_tracker/features/subscriptions/application/purchase_controller.dart';
import 'package:money_tracker/features/subscriptions/domain/offering.dart';
import 'package:money_tracker/features/subscriptions/domain/purchase_result.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/revenuecat_sdk_adapter.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';

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

void main() {
  late InMemoryRevenueCatSdkAdapter sdkAdapter;
  late EntitlementProvider entitlementProvider;
  late PurchaseController controller;

  final premiumResponse = jsonEncode({
    'tier': 'Premium',
    'featureKeys': [
      'BankSync',
      'PremiumInsights',
      'UnlimitedBudgets',
      'UnlimitedBillReminders',
      'ExportData',
    ],
    'trialExpiresAtUtc': null,
    'currentPeriodEndUtc': '2026-04-01T00:00:00Z',
  });

  setUp(() {
    sdkAdapter = InMemoryRevenueCatSdkAdapter(
      offering: Offering(
        identifier: 'default',
        packages: [_testAnnualPackage, _testMonthlyPackage],
      ),
    );

    final httpClient = StubHttpClient(
      statusCode: 200,
      responseBody: premiumResponse,
    );

    final gateway = SubscriptionGateway(
      apiBaseUrl: Uri.parse('https://api.example.com'),
      tokenProvider: () => 'test-token',
      httpClient: httpClient,
    );

    entitlementProvider = EntitlementProvider(
      gateway: gateway,
      cacheTtl: const Duration(minutes: 5),
    );

    controller = PurchaseController(
      revenueCatSdk: sdkAdapter,
      entitlementProvider: entitlementProvider,
      householdId: 'household-1',
    );
  });

  group('PurchaseController', () {
    test('starts in idle state', () {
      expect(controller.state, PurchaseFlowState.idle);
      expect(controller.lastResult, isNull);
      expect(controller.errorMessage, isNull);
      expect(controller.isPurchasing, false);
    });

    // P4-4-UNIT-04: PurchaseController initiates purchase for selected package.
    test('initiates purchase for given package', () async {
      await controller.purchase(_testAnnualPackage);

      expect(sdkAdapter.purchaseCallCount, 1);
      expect(sdkAdapter.lastPurchasedPackage, _testAnnualPackage);
    });

    // P4-4-UNIT-05: PurchaseController handles successful purchase.
    test('successful purchase transitions to success state', () async {
      final result = await controller.purchase(_testAnnualPackage);

      expect(result, isA<PurchaseSuccess>());
      expect(controller.state, PurchaseFlowState.success);
      expect(controller.lastResult, isA<PurchaseSuccess>());
    });

    test('successful purchase refreshes entitlements', () async {
      await controller.purchase(_testAnnualPackage);

      // Entitlements should have been refreshed (cache invalidated + re-fetched).
      expect(entitlementProvider.entitlements.tier.name, 'premium');
    });

    // P4-4-UNIT-06: PurchaseController handles failed purchase.
    test('failed purchase transitions to error state', () async {
      sdkAdapter = InMemoryRevenueCatSdkAdapter(
        purchaseResult: const PurchaseFailed(
          errorMessage: 'Store unavailable',
          isRetryable: true,
        ),
      );

      controller = PurchaseController(
        revenueCatSdk: sdkAdapter,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final result = await controller.purchase(_testMonthlyPackage);

      expect(result, isA<PurchaseFailed>());
      expect(controller.state, PurchaseFlowState.error);
      expect(controller.errorMessage, 'Store unavailable');
    });

    // P4-4-UNIT-07: PurchaseController handles user cancellation.
    test('cancelled purchase returns to idle without error', () async {
      sdkAdapter = InMemoryRevenueCatSdkAdapter(
        purchaseResult: const PurchaseCancelled(),
      );

      controller = PurchaseController(
        revenueCatSdk: sdkAdapter,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final result = await controller.purchase(_testMonthlyPackage);

      expect(result, isA<PurchaseCancelled>());
      expect(controller.state, PurchaseFlowState.cancelled);
      expect(controller.errorMessage, isNull);
    });

    // P4-4-UNIT-08: PurchaseController handles deferred/pending purchase.
    test('pending purchase transitions to pending state', () async {
      sdkAdapter = InMemoryRevenueCatSdkAdapter(
        purchaseResult: const PurchasePending(
          message: 'Awaiting parental approval.',
        ),
      );

      controller = PurchaseController(
        revenueCatSdk: sdkAdapter,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final result = await controller.purchase(_testMonthlyPackage);

      expect(result, isA<PurchasePending>());
      expect(controller.state, PurchaseFlowState.pending);
    });

    // P4-4-UNIT-14: Purchase flow handles edge cases.
    test('SDK exception transitions to error state', () async {
      sdkAdapter = InMemoryRevenueCatSdkAdapter(shouldThrow: true);

      controller = PurchaseController(
        revenueCatSdk: sdkAdapter,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final result = await controller.purchase(_testAnnualPackage);

      expect(result, isA<PurchaseFailed>());
      expect(controller.state, PurchaseFlowState.error);
      expect(controller.errorMessage, isNotNull);
    });

    test('notifies listeners on state transitions', () async {
      int notifyCount = 0;
      controller.addListener(() => notifyCount++);

      await controller.purchase(_testAnnualPackage);

      // At least 2: purchasing start and success/error end.
      expect(notifyCount, greaterThanOrEqualTo(2));
    });

    test('isPurchasing is true during purchase', () async {
      bool wasPurchasing = false;
      controller.addListener(() {
        if (controller.isPurchasing) {
          wasPurchasing = true;
        }
      });

      await controller.purchase(_testAnnualPackage);

      expect(wasPurchasing, true);
      expect(controller.isPurchasing, false);
    });

    test('reset returns to idle state', () async {
      await controller.purchase(_testAnnualPackage);
      expect(controller.state, PurchaseFlowState.success);

      controller.reset();

      expect(controller.state, PurchaseFlowState.idle);
      expect(controller.lastResult, isNull);
      expect(controller.errorMessage, isNull);
    });
  });

  group('PurchaseResult sealed class', () {
    test('PurchaseSuccess contains productId and isActive', () {
      const result = PurchaseSuccess(productId: 'test', isActive: true);
      expect(result.productId, 'test');
      expect(result.isActive, true);
    });

    test('PurchaseCancelled is a valid result', () {
      const result = PurchaseCancelled();
      expect(result, isA<PurchaseResult>());
    });

    test('PurchasePending has default message', () {
      const result = PurchasePending();
      expect(result.message, isNotEmpty);
    });

    test('PurchaseFailed contains errorMessage and isRetryable', () {
      const result = PurchaseFailed(
        errorMessage: 'Network error',
        isRetryable: true,
      );
      expect(result.errorMessage, 'Network error');
      expect(result.isRetryable, true);
    });

    test('PurchaseFailed isRetryable defaults to true', () {
      const result = PurchaseFailed(errorMessage: 'Error');
      expect(result.isRetryable, true);
    });
  });
}
