import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/subscriptions/application/offerings_provider.dart';
import 'package:money_tracker/features/subscriptions/domain/offering.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/revenuecat_sdk_adapter.dart';

Offering _buildDefaultOffering() {
  return const Offering(
    identifier: 'default',
    packages: [
      Package(
        identifier: '\$rc_annual',
        packageType: PackageType.annual,
        productId: 'premium_annual',
        priceString: '\$49.99',
        priceAmountMicros: 49990000,
        currencyCode: 'USD',
        period: BillingPeriod.annual,
      ),
      Package(
        identifier: '\$rc_monthly',
        packageType: PackageType.monthly,
        productId: 'premium_monthly',
        priceString: '\$5.99',
        priceAmountMicros: 5990000,
        currencyCode: 'USD',
        period: BillingPeriod.monthly,
      ),
    ],
  );
}

void main() {
  late InMemoryRevenueCatSdkAdapter sdkAdapter;
  late OfferingsProvider provider;

  group('OfferingsProvider', () {
    setUp(() {
      sdkAdapter = InMemoryRevenueCatSdkAdapter(
        offering: _buildDefaultOffering(),
      );
      provider = OfferingsProvider(
        revenueCatSdk: sdkAdapter,
        cacheTtl: const Duration(minutes: 15),
      );
    });

    test('starts with null offering and not loading', () {
      expect(provider.offering, isNull);
      expect(provider.isLoading, false);
      expect(provider.errorMessage, isNull);
    });

    // P4-4-UNIT-01: OfferingsProvider parses RevenueCat offering response.
    test('fetchOffering returns offering with correct packages', () async {
      final result = await provider.fetchOffering();

      expect(result, isNotNull);
      expect(result!.identifier, 'default');
      expect(result.packages.length, 2);

      final annual = result.annual;
      expect(annual, isNotNull);
      expect(annual!.productId, 'premium_annual');
      expect(annual.priceAmountMicros, 49990000);
      expect(annual.packageType, PackageType.annual);

      final monthly = result.monthly;
      expect(monthly, isNotNull);
      expect(monthly!.productId, 'premium_monthly');
      expect(monthly.priceAmountMicros, 5990000);
    });

    // P4-4-UNIT-09: OfferingsProvider cache hit returns cached without SDK call.
    test('cache hit returns cached offerings without SDK call', () async {
      await provider.fetchOffering();
      expect(sdkAdapter.getOfferingsCallCount, 1);

      // Second call should use cache.
      final result = await provider.fetchOffering();
      expect(sdkAdapter.getOfferingsCallCount, 1);
      expect(result, isNotNull);
      expect(result!.packages.length, 2);
    });

    test('forceRefresh bypasses cache', () async {
      await provider.fetchOffering();
      expect(sdkAdapter.getOfferingsCallCount, 1);

      await provider.fetchOffering(forceRefresh: true);
      expect(sdkAdapter.getOfferingsCallCount, 2);
    });

    test('invalidateCache forces fresh fetch on next access', () async {
      await provider.fetchOffering();
      expect(sdkAdapter.getOfferingsCallCount, 1);

      provider.invalidateCache();

      await provider.fetchOffering();
      expect(sdkAdapter.getOfferingsCallCount, 2);
    });

    test('notifies listeners on fetch', () async {
      int notifyCount = 0;
      provider.addListener(() => notifyCount++);

      await provider.fetchOffering();

      // At least 2: loading start and loading end.
      expect(notifyCount, greaterThanOrEqualTo(2));
    });

    test('isLoading is false after fetch completes', () async {
      await provider.fetchOffering();
      expect(provider.isLoading, false);
    });

    test('SDK error sets error message', () async {
      final failingSdk = InMemoryRevenueCatSdkAdapter(shouldThrow: true);
      final failingProvider = OfferingsProvider(revenueCatSdk: failingSdk);

      await failingProvider.fetchOffering();

      expect(failingProvider.errorMessage, isNotNull);
      expect(failingProvider.offering, isNull);
    });

    test('null offering from SDK is handled gracefully', () async {
      final emptySdk = InMemoryRevenueCatSdkAdapter();
      final emptyProvider = OfferingsProvider(revenueCatSdk: emptySdk);

      final result = await emptyProvider.fetchOffering();

      expect(result, isNull);
      expect(emptyProvider.errorMessage, isNull);
    });
  });

  group('Offering domain', () {
    test('annual getter returns annual package', () {
      final offering = _buildDefaultOffering();
      expect(offering.annual, isNotNull);
      expect(offering.annual!.packageType, PackageType.annual);
    });

    test('monthly getter returns monthly package', () {
      final offering = _buildDefaultOffering();
      expect(offering.monthly, isNotNull);
      expect(offering.monthly!.packageType, PackageType.monthly);
    });

    test('annual getter returns null when no annual package', () {
      const offering = Offering(
        identifier: 'default',
        packages: [
          Package(
            identifier: '\$rc_monthly',
            packageType: PackageType.monthly,
            productId: 'premium_monthly',
            priceString: '\$5.99',
            priceAmountMicros: 5990000,
            currencyCode: 'USD',
            period: BillingPeriod.monthly,
          ),
        ],
      );
      expect(offering.annual, isNull);
    });

    // P4-4-UNIT-03: PlanCard calculates per-month equivalent for annual.
    test('monthlyEquivalentMicros divides annual by 12', () {
      const annual = Package(
        identifier: '\$rc_annual',
        packageType: PackageType.annual,
        productId: 'premium_annual',
        priceString: '\$49.99',
        priceAmountMicros: 49990000,
        currencyCode: 'USD',
        period: BillingPeriod.annual,
      );

      // 49990000 / 12 = 4165833 (rounded)
      expect(annual.monthlyEquivalentMicros, 4165833);
    });

    // P4-4-UNIT-02: PlanCard calculates annual savings percentage.
    test('savingsPercentVsMonthly calculates correct percentage', () {
      const annual = Package(
        identifier: '\$rc_annual',
        packageType: PackageType.annual,
        productId: 'premium_annual',
        priceString: '\$49.99',
        priceAmountMicros: 49990000,
        currencyCode: 'USD',
        period: BillingPeriod.annual,
      );

      // Monthly at $5.99 = 5990000 micros * 12 = 71880000 yearly
      // Savings = (71880000 - 49990000) / 71880000 * 100 = 30%
      final savings = annual.savingsPercentVsMonthly(5990000);
      expect(savings, 30);
    });

    test('savingsPercentVsMonthly returns 0 for monthly package', () {
      const monthly = Package(
        identifier: '\$rc_monthly',
        packageType: PackageType.monthly,
        productId: 'premium_monthly',
        priceString: '\$5.99',
        priceAmountMicros: 5990000,
        currencyCode: 'USD',
        period: BillingPeriod.monthly,
      );

      expect(monthly.savingsPercentVsMonthly(5990000), 0);
    });

    test('savingsPercentVsMonthly returns 0 when monthly price is zero', () {
      const annual = Package(
        identifier: '\$rc_annual',
        packageType: PackageType.annual,
        productId: 'premium_annual',
        priceString: '\$49.99',
        priceAmountMicros: 49990000,
        currencyCode: 'USD',
        period: BillingPeriod.annual,
      );

      expect(annual.savingsPercentVsMonthly(0), 0);
    });
  });

  group('IntroPrice', () {
    test('isFreeTrial is true when price is zero', () {
      const intro = IntroPrice(
        priceString: '\$0.00',
        priceAmountMicros: 0,
        periodDays: 14,
        cycles: 1,
      );
      expect(intro.isFreeTrial, true);
      expect(intro.totalTrialDays, 14);
    });

    test('isFreeTrial is false when price is non-zero', () {
      const intro = IntroPrice(
        priceString: '\$0.99',
        priceAmountMicros: 990000,
        periodDays: 7,
        cycles: 1,
      );
      expect(intro.isFreeTrial, false);
    });
  });
}
