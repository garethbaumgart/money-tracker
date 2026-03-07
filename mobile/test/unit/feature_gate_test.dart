import 'dart:convert';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/subscriptions/application/entitlement_provider.dart';
import 'package:money_tracker/features/subscriptions/domain/feature_key.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';
import 'package:money_tracker/features/subscriptions/presentation/feature_gate.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({required this.responseBody});

  final String responseBody;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    return HttpResponse(statusCode: 200, body: responseBody);
  }
}

void main() {
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

  final freeResponse = jsonEncode({
    'tier': 'Free',
    'featureKeys': <String>[],
    'trialExpiresAtUtc': null,
    'currentPeriodEndUtc': null,
  });

  group('FeatureGate', () {
    testWidgets('renders entitled widget when feature is available',
        (tester) async {
      // P4-2-UNIT-10: FeatureGate with entitled user renders premium content widget
      final gateway = SubscriptionGateway(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: StubHttpClient(responseBody: premiumResponse),
      );
      final provider = EntitlementProvider(gateway: gateway);
      addTearDown(provider.dispose);

      await provider.fetchEntitlements('household-1');

      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: FeatureGate(
            feature: FeatureKey.bankSync,
            entitlementProvider: provider,
            entitled: const Text('Premium Content'),
            fallback: const Text('Upgrade Now'),
          ),
        ),
      );

      expect(find.text('Premium Content'), findsOneWidget);
      expect(find.text('Upgrade Now'), findsNothing);
    });

    testWidgets('renders fallback widget when feature is not available',
        (tester) async {
      // P4-2-UNIT-11: FeatureGate with non-entitled user renders upgrade CTA widget
      final gateway = SubscriptionGateway(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: StubHttpClient(responseBody: freeResponse),
      );
      final provider = EntitlementProvider(gateway: gateway);
      addTearDown(provider.dispose);

      await provider.fetchEntitlements('household-1');

      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: FeatureGate(
            feature: FeatureKey.bankSync,
            entitlementProvider: provider,
            entitled: const Text('Premium Content'),
            fallback: const Text('Upgrade Now'),
          ),
        ),
      );

      expect(find.text('Upgrade Now'), findsOneWidget);
      expect(find.text('Premium Content'), findsNothing);
    });

    testWidgets('renders fallback by default before entitlements are fetched',
        (tester) async {
      final gateway = SubscriptionGateway(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: StubHttpClient(responseBody: premiumResponse),
      );
      final provider = EntitlementProvider(gateway: gateway);
      addTearDown(provider.dispose);

      // Do NOT fetch entitlements - should default to free

      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: FeatureGate(
            feature: FeatureKey.bankSync,
            entitlementProvider: provider,
            entitled: const Text('Premium Content'),
            fallback: const Text('Upgrade Now'),
          ),
        ),
      );

      expect(find.text('Upgrade Now'), findsOneWidget);
      expect(find.text('Premium Content'), findsNothing);
    });

    testWidgets('updates when entitlements change', (tester) async {
      final gateway = SubscriptionGateway(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: StubHttpClient(responseBody: premiumResponse),
      );
      final provider = EntitlementProvider(gateway: gateway);
      addTearDown(provider.dispose);

      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: FeatureGate(
            feature: FeatureKey.bankSync,
            entitlementProvider: provider,
            entitled: const Text('Premium Content'),
            fallback: const Text('Upgrade Now'),
          ),
        ),
      );

      // Initially free
      expect(find.text('Upgrade Now'), findsOneWidget);

      // Fetch entitlements (becomes premium)
      await provider.fetchEntitlements('household-1');
      await tester.pump();

      expect(find.text('Premium Content'), findsOneWidget);
    });
  });
}
