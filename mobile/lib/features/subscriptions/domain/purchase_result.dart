/// Result of a purchase attempt — a sealed type covering all outcomes.
sealed class PurchaseResult {
  const PurchaseResult();
}

/// Purchase completed successfully.
class PurchaseSuccess extends PurchaseResult {
  const PurchaseSuccess({
    required this.productId,
    required this.isActive,
  });

  /// The product ID that was purchased.
  final String productId;

  /// Whether the subscription is now active.
  final bool isActive;
}

/// User cancelled the purchase (dismissed the store sheet).
class PurchaseCancelled extends PurchaseResult {
  const PurchaseCancelled();
}

/// Purchase is pending (deferred transaction, e.g. parental controls).
class PurchasePending extends PurchaseResult {
  const PurchasePending({
    this.message = 'Your purchase is pending approval.',
  });

  /// Human-readable explanation of the pending state.
  final String message;
}

/// Purchase failed with an error.
class PurchaseFailed extends PurchaseResult {
  const PurchaseFailed({
    required this.errorMessage,
    this.isRetryable = true,
  });

  /// Human-readable error description.
  final String errorMessage;

  /// Whether the user should be offered a retry option.
  final bool isRetryable;
}
