import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/subscriptions/application/entitlement_provider.dart';
import 'package:money_tracker/features/subscriptions/domain/entitlement_set.dart';
import 'package:money_tracker/features/subscriptions/domain/feature_key.dart';
import 'package:money_tracker/features/subscriptions/domain/subscription_tier.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({
    required this.statusCode,
    required this.responseBody,
  });

  final int statusCode;
  final String responseBody;
  int callCount = 0;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    callCount++;
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    callCount++;
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }
}

void main() {
  late StubHttpClient httpClient;
  late SubscriptionGateway gateway;
  late EntitlementProvider provider;

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
    httpClient = StubHttpClient(
      statusCode: 200,
      responseBody: premiumResponse,
    );
    gateway = SubscriptionGateway(
      apiBaseUrl: Uri.parse('https://api.example.com'),
      tokenProvider: () => 'test-token',
      httpClient: httpClient,
    );
    provider = EntitlementProvider(
      gateway: gateway,
      cacheTtl: const Duration(minutes: 5),
    );
  });

  group('EntitlementProvider', () {
    test('starts with Free tier entitlements', () {
      expect(provider.entitlements.tier, SubscriptionTier.free);
      expect(provider.hasFeature(FeatureKey.bankSync), false);
    });

    test('fetchEntitlements returns entitlements from API', () async {
      final result = await provider.fetchEntitlements('household-1');

      expect(result.tier, SubscriptionTier.premium);
      expect(result.hasFeature(FeatureKey.bankSync), true);
      expect(result.hasFeature(FeatureKey.premiumInsights), true);
      expect(result.hasFeature(FeatureKey.unlimitedBudgets), true);
      expect(result.hasFeature(FeatureKey.unlimitedBillReminders), true);
      expect(result.hasFeature(FeatureKey.exportData), true);
      expect(httpClient.callCount, 1);
    });

    test('cache hit within TTL does not make API call', () async {
      // P4-2-UNIT-08: EntitlementProvider cache hit within TTL returns cached result
      await provider.fetchEntitlements('household-1');
      expect(httpClient.callCount, 1);

      // Second call should use cache
      final result = await provider.fetchEntitlements('household-1');
      expect(httpClient.callCount, 1);
      expect(result.tier, SubscriptionTier.premium);
    });

    test('invalidateCache forces fresh API call', () async {
      // P4-2-UNIT-09: EntitlementProvider cache miss after TTL makes API call
      await provider.fetchEntitlements('household-1');
      expect(httpClient.callCount, 1);

      provider.invalidateCache();

      await provider.fetchEntitlements('household-1');
      expect(httpClient.callCount, 2);
    });

    test('hasFeature reflects current entitlements', () async {
      expect(provider.hasFeature(FeatureKey.bankSync), false);

      await provider.fetchEntitlements('household-1');

      expect(provider.hasFeature(FeatureKey.bankSync), true);
      expect(provider.hasFeature(FeatureKey.exportData), true);
    });

    test('notifies listeners on fetch', () async {
      int notifyCount = 0;
      provider.addListener(() => notifyCount++);

      await provider.fetchEntitlements('household-1');

      // Should notify at least twice: loading start and loading end
      expect(notifyCount, greaterThanOrEqualTo(2));
    });

    test('isLoading is false after fetch completes', () async {
      await provider.fetchEntitlements('household-1');

      expect(provider.isLoading, false);
    });
  });

  group('EntitlementSet.fromApiResponse', () {
    test('parses premium response correctly', () {
      final set = EntitlementSet.fromApiResponse(
        tier: 'Premium',
        featureKeys: ['BankSync', 'PremiumInsights', 'UnlimitedBudgets',
            'UnlimitedBillReminders', 'ExportData'],
      );

      expect(set.tier, SubscriptionTier.premium);
      expect(set.featureKeys.length, 5);
      expect(set.hasFeature(FeatureKey.bankSync), true);
    });

    test('parses free response correctly', () {
      final set = EntitlementSet.fromApiResponse(
        tier: 'Free',
        featureKeys: [],
      );

      expect(set.tier, SubscriptionTier.free);
      expect(set.featureKeys.isEmpty, true);
    });

    test('parses trial response with expiry', () {
      final expiry = DateTime.parse('2026-03-15T00:00:00Z');
      final set = EntitlementSet.fromApiResponse(
        tier: 'Trial',
        featureKeys: ['BankSync', 'PremiumInsights', 'UnlimitedBudgets',
            'UnlimitedBillReminders', 'ExportData'],
        trialExpiresAtUtc: expiry,
      );

      expect(set.tier, SubscriptionTier.trial);
      expect(set.trialExpiresAtUtc, expiry);
      expect(set.featureKeys.length, 5);
    });
  });
}
