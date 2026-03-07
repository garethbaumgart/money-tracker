enum SubscriptionTier {
  free,
  trial,
  premium;

  static SubscriptionTier fromString(String value) {
    return SubscriptionTier.values.firstWhere(
      (tier) => tier.name.toLowerCase() == value.toLowerCase(),
      orElse: () => SubscriptionTier.free,
    );
  }
}
