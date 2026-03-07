import 'package:flutter/foundation.dart';

/// Result of a RevenueCat restore purchases operation.
class RestoreResult {
  const RestoreResult({
    required this.isActive,
    required this.appUserId,
    this.productId,
    this.expiresAt,
  });

  final bool isActive;
  final String appUserId;
  final String? productId;
  final DateTime? expiresAt;
}

/// Abstraction over the RevenueCat SDK.
/// Enables testability by allowing in-memory/stub implementations.
abstract class RevenueCatSdkAdapter {
  /// Restores purchases via the platform app store.
  /// Returns the current subscriber state from RevenueCat.
  Future<RestoreResult> restorePurchases();

  /// Returns the current RevenueCat app user ID.
  Future<String> getAppUserId();

  /// Checks if the user has an active subscription.
  Future<bool> hasActiveSubscription();
}

/// In-memory implementation for testing.
class InMemoryRevenueCatSdkAdapter implements RevenueCatSdkAdapter {
  InMemoryRevenueCatSdkAdapter({
    this.appUserId = 'test-user-id',
    this.isActive = false,
    this.productId,
    this.expiresAt,
    this.shouldThrow = false,
  });

  final String appUserId;
  final bool isActive;
  final String? productId;
  final DateTime? expiresAt;
  final bool shouldThrow;
  int restoreCallCount = 0;

  @override
  Future<RestoreResult> restorePurchases() async {
    restoreCallCount++;
    if (shouldThrow) {
      throw Exception('RevenueCat SDK error');
    }
    return RestoreResult(
      isActive: isActive,
      appUserId: appUserId,
      productId: productId,
      expiresAt: expiresAt,
    );
  }

  @override
  Future<String> getAppUserId() async {
    return appUserId;
  }

  @override
  Future<bool> hasActiveSubscription() async {
    return isActive;
  }
}
