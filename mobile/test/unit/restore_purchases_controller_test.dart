import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/subscriptions/application/entitlement_provider.dart';
import 'package:money_tracker/features/subscriptions/application/restore_purchases_controller.dart';
import 'package:money_tracker/features/subscriptions/domain/subscription_tier.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/revenuecat_sdk_adapter.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({
    required this.getStatusCode,
    required this.getResponseBody,
    this.postStatusCode = 200,
    this.postResponseBody = '{}',
  });

  final int getStatusCode;
  final String getResponseBody;
  final int postStatusCode;
  final String postResponseBody;
  int getCallCount = 0;
  int postCallCount = 0;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    getCallCount++;
    return HttpResponse(statusCode: getStatusCode, body: getResponseBody);
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    postCallCount++;
    return HttpResponse(statusCode: postStatusCode, body: postResponseBody);
  }
}

void main() {
  late InMemoryRevenueCatSdkAdapter sdkAdapter;
  late StubHttpClient httpClient;
  late SubscriptionGateway gateway;
  late EntitlementProvider entitlementProvider;
  late RestorePurchasesController controller;

  final premiumEntitlementResponse = jsonEncode({
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

  final restoreActiveResponse = jsonEncode({
    'status': 'Active',
    'tier': 'Premium',
    'featureKeys': [
      'BankSync',
      'PremiumInsights',
      'UnlimitedBudgets',
      'UnlimitedBillReminders',
      'ExportData',
    ],
    'currentPeriodEndUtc': '2026-04-01T00:00:00Z',
  });

  final restoreNoneResponse = jsonEncode({
    'status': 'None',
    'tier': 'Free',
    'featureKeys': <String>[],
    'currentPeriodEndUtc': null,
  });

  setUp(() {
    sdkAdapter = InMemoryRevenueCatSdkAdapter(
      appUserId: 'test-rc-user',
      isActive: true,
      productId: 'premium_monthly',
    );

    httpClient = StubHttpClient(
      getStatusCode: 200,
      getResponseBody: premiumEntitlementResponse,
      postStatusCode: 200,
      postResponseBody: restoreActiveResponse,
    );

    gateway = SubscriptionGateway(
      apiBaseUrl: Uri.parse('https://api.example.com'),
      tokenProvider: () => 'test-token',
      httpClient: httpClient,
    );

    entitlementProvider = EntitlementProvider(
      gateway: gateway,
      cacheTtl: const Duration(minutes: 5),
    );

    controller = RestorePurchasesController(
      revenueCatSdk: sdkAdapter,
      gateway: gateway,
      entitlementProvider: entitlementProvider,
      householdId: 'household-1',
    );
  });

  group('RestorePurchasesController', () {
    test('successful restore calls SDK then backend', () async {
      // AC-11: Calls RevenueCat SDK restorePurchases() then backend POST
      final outcome = await controller.restorePurchases();

      expect(outcome.isSuccess, true);
      expect(outcome.status, 'Active');
      expect(outcome.tier, 'Premium');
      expect(sdkAdapter.restoreCallCount, 1);
      expect(httpClient.postCallCount, 1);
    });

    test('isRestoring is true during restore and false after', () async {
      bool wasRestoringDuringCall = false;
      controller.addListener(() {
        if (controller.isRestoring) {
          wasRestoringDuringCall = true;
        }
      });

      await controller.restorePurchases();

      expect(wasRestoringDuringCall, true);
      expect(controller.isRestoring, false);
    });

    test('notifies listeners on start and end', () async {
      int notifyCount = 0;
      controller.addListener(() => notifyCount++);

      await controller.restorePurchases();

      // At least 2: start (isRestoring=true) and end (isRestoring=false)
      expect(notifyCount, greaterThanOrEqualTo(2));
    });

    test('SDK error results in failure outcome', () async {
      final failingSdk = InMemoryRevenueCatSdkAdapter(
        appUserId: 'test-user',
        shouldThrow: true,
      );

      controller = RestorePurchasesController(
        revenueCatSdk: failingSdk,
        gateway: gateway,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final outcome = await controller.restorePurchases();

      expect(outcome.isSuccess, false);
      expect(outcome.errorMessage, isNotNull);
    });

    test('backend error results in failure outcome', () async {
      httpClient = StubHttpClient(
        getStatusCode: 200,
        getResponseBody: premiumEntitlementResponse,
        postStatusCode: 500,
        postResponseBody: '{"error":"server error"}',
      );

      gateway = SubscriptionGateway(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: httpClient,
      );

      controller = RestorePurchasesController(
        revenueCatSdk: sdkAdapter,
        gateway: gateway,
        entitlementProvider: entitlementProvider,
        householdId: 'household-1',
      );

      final outcome = await controller.restorePurchases();

      expect(outcome.isSuccess, false);
      expect(outcome.errorMessage, isNotNull);
    });

    test('invalidates entitlement cache after restore', () async {
      // First, populate the cache
      await entitlementProvider.fetchEntitlements('household-1');
      expect(httpClient.getCallCount, 1);

      // Restore should invalidate cache and re-fetch
      await controller.restorePurchases();

      // GET call count should increase (cache was invalidated and re-fetched)
      expect(httpClient.getCallCount, greaterThan(1));
    });

    test('lastOutcome is set after restore', () async {
      expect(controller.lastOutcome, isNull);

      await controller.restorePurchases();

      expect(controller.lastOutcome, isNotNull);
      expect(controller.lastOutcome!.isSuccess, true);
    });
  });
}
