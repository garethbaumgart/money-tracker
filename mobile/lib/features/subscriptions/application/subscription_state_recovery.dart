import 'package:flutter/foundation.dart';
import '../infrastructure/revenuecat_sdk_adapter.dart';
import '../infrastructure/subscription_gateway.dart';
import 'entitlement_provider.dart';
import 'restore_purchases_controller.dart';

/// Handles subscription state recovery on app launch.
/// AC-12: Reconciles local state with RevenueCat on app launch.
class SubscriptionStateRecovery {
  SubscriptionStateRecovery({
    required RevenueCatSdkAdapter revenueCatSdk,
    required SubscriptionGateway gateway,
    required EntitlementProvider entitlementProvider,
    required String householdId,
  })  : _revenueCatSdk = revenueCatSdk,
        _gateway = gateway,
        _entitlementProvider = entitlementProvider,
        _householdId = householdId;

  final RevenueCatSdkAdapter _revenueCatSdk;
  final SubscriptionGateway _gateway;
  final EntitlementProvider _entitlementProvider;
  final String _householdId;

  bool _hasRecovered = false;
  bool get hasRecovered => _hasRecovered;

  /// Runs on app launch to reconcile subscription state.
  /// Checks RevenueCat SDK for current state, and if it differs
  /// from the local cache, triggers a restore flow.
  Future<void> recoverIfNeeded() async {
    if (_hasRecovered) return;

    try {
      // Check if RevenueCat SDK reports an active subscription
      final sdkHasActive = await _revenueCatSdk.hasActiveSubscription();
      final localTier = _entitlementProvider.entitlements.tier;

      // If local state differs from SDK state, reconcile
      final localIsActive = localTier.name != 'free';
      if (sdkHasActive != localIsActive) {
        final appUserId = await _revenueCatSdk.getAppUserId();
        await _gateway.restorePurchases(
          householdId: _householdId,
          revenueCatAppUserId: appUserId,
        );

        // Refresh entitlements after recovery
        _entitlementProvider.invalidateCache();
        await _entitlementProvider.fetchEntitlements(_householdId);
      }

      _hasRecovered = true;
    } catch (e) {
      // Recovery is best-effort; log and continue.
      // The user can manually restore later.
      debugPrint('Subscription state recovery failed: $e');
      _hasRecovered = true;
    }
  }
}
