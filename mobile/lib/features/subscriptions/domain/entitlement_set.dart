import 'feature_key.dart';
import 'subscription_tier.dart';

class EntitlementSet {
  const EntitlementSet._({
    required this.tier,
    required this.featureKeys,
    this.trialExpiresAtUtc,
    this.currentPeriodEndUtc,
  });

  final SubscriptionTier tier;
  final Set<FeatureKey> featureKeys;
  final DateTime? trialExpiresAtUtc;
  final DateTime? currentPeriodEndUtc;

  bool hasFeature(FeatureKey feature) => featureKeys.contains(feature);

  factory EntitlementSet.forTier(SubscriptionTier tier) {
    switch (tier) {
      case SubscriptionTier.premium:
      case SubscriptionTier.trial:
        return EntitlementSet._(
          tier: tier,
          featureKeys: const {
            FeatureKey.bankSync,
            FeatureKey.premiumInsights,
            FeatureKey.unlimitedBudgets,
            FeatureKey.unlimitedBillReminders,
            FeatureKey.exportData,
          },
        );
      case SubscriptionTier.free:
        return EntitlementSet._(
          tier: tier,
          featureKeys: const {},
        );
    }
  }

  factory EntitlementSet.fromApiResponse({
    required String tier,
    required List<String> featureKeys,
    DateTime? trialExpiresAtUtc,
    DateTime? currentPeriodEndUtc,
  }) {
    final parsedTier = SubscriptionTier.fromString(tier);
    final parsedFeatures = <FeatureKey>{};
    for (final key in featureKeys) {
      final parsed = FeatureKey.fromString(key);
      if (parsed != null) {
        parsedFeatures.add(parsed);
      }
    }

    return EntitlementSet._(
      tier: parsedTier,
      featureKeys: parsedFeatures,
      trialExpiresAtUtc: trialExpiresAtUtc,
      currentPeriodEndUtc: currentPeriodEndUtc,
    );
  }

  factory EntitlementSet.free() {
    return EntitlementSet.forTier(SubscriptionTier.free);
  }
}
