/// A RevenueCat Offering — a collection of packages available for purchase.
class Offering {
  const Offering({
    required this.identifier,
    required this.packages,
    this.metadata = const {},
  });

  /// Unique identifier for this offering (e.g. "default").
  final String identifier;

  /// The packages available within this offering.
  final List<Package> packages;

  /// Server-side metadata attached to this offering.
  final Map<String, String> metadata;

  /// Returns the annual package if one exists.
  Package? get annual => packages.cast<Package?>().firstWhere(
        (p) => p!.packageType == PackageType.annual,
        orElse: () => null,
      );

  /// Returns the monthly package if one exists.
  Package? get monthly => packages.cast<Package?>().firstWhere(
        (p) => p!.packageType == PackageType.monthly,
        orElse: () => null,
      );
}

/// A purchasable package within an offering.
class Package {
  const Package({
    required this.identifier,
    required this.packageType,
    required this.productId,
    required this.priceString,
    required this.priceAmountMicros,
    required this.currencyCode,
    required this.period,
    this.introPrice,
  });

  /// Unique identifier for this package (e.g. "\$rc_annual").
  final String identifier;

  /// The type of package (annual, monthly, etc.).
  final PackageType packageType;

  /// The underlying product ID from the app store.
  final String productId;

  /// Localized price string (e.g. "\$49.99").
  final String priceString;

  /// Price in micros (e.g. 49990000 for \$49.99).
  final int priceAmountMicros;

  /// ISO 4217 currency code (e.g. "USD").
  final String currencyCode;

  /// Billing period for this package.
  final BillingPeriod period;

  /// Introductory price info, if available (e.g. free trial).
  final IntroPrice? introPrice;

  /// Calculates the per-month price in micros for annual packages.
  int get monthlyEquivalentMicros {
    if (packageType == PackageType.annual) {
      return (priceAmountMicros / 12).round();
    }
    return priceAmountMicros;
  }

  /// Calculates the savings percentage compared to a monthly package.
  /// Returns 0 if this is not an annual package or monthlyPrice is zero.
  int savingsPercentVsMonthly(int monthlyPriceMicros) {
    if (packageType != PackageType.annual || monthlyPriceMicros <= 0) {
      return 0;
    }
    final yearlyAtMonthlyRate = monthlyPriceMicros * 12;
    final savings = yearlyAtMonthlyRate - priceAmountMicros;
    return ((savings / yearlyAtMonthlyRate) * 100).round();
  }
}

/// Type of subscription package.
enum PackageType {
  annual,
  monthly,
  weekly,
  lifetime,
  custom;

  static PackageType fromString(String value) {
    return PackageType.values.firstWhere(
      (t) => t.name.toLowerCase() == value.toLowerCase(),
      orElse: () => PackageType.custom,
    );
  }
}

/// Billing period for a subscription.
enum BillingPeriod {
  monthly,
  annual,
  weekly,
  lifetime;

  String get displayLabel {
    switch (this) {
      case BillingPeriod.monthly:
        return 'month';
      case BillingPeriod.annual:
        return 'year';
      case BillingPeriod.weekly:
        return 'week';
      case BillingPeriod.lifetime:
        return 'lifetime';
    }
  }
}

/// Introductory pricing information (e.g., free trial).
class IntroPrice {
  const IntroPrice({
    required this.priceString,
    required this.priceAmountMicros,
    required this.periodDays,
    required this.cycles,
  });

  /// Localized price string for the intro period ("\$0.00" for free trial).
  final String priceString;

  /// Price in micros for the intro period.
  final int priceAmountMicros;

  /// Duration of each intro period in days.
  final int periodDays;

  /// Number of intro billing cycles.
  final int cycles;

  /// Whether this intro is a free trial.
  bool get isFreeTrial => priceAmountMicros == 0;

  /// Total trial days.
  int get totalTrialDays => periodDays * cycles;
}
