import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../infrastructure/revenuecat_sdk_adapter.dart';
import '../infrastructure/subscription_gateway.dart';
import 'entitlement_provider.dart';

/// Result of a restore purchases operation.
class RestorePurchasesOutcome {
  const RestorePurchasesOutcome._({
    required this.isSuccess,
    this.status,
    this.tier,
    this.errorMessage,
  });

  final bool isSuccess;
  final String? status;
  final String? tier;
  final String? errorMessage;

  factory RestorePurchasesOutcome.success({
    required String status,
    required String tier,
  }) {
    return RestorePurchasesOutcome._(
      isSuccess: true,
      status: status,
      tier: tier,
    );
  }

  factory RestorePurchasesOutcome.failure(String errorMessage) {
    return RestorePurchasesOutcome._(
      isSuccess: false,
      errorMessage: errorMessage,
    );
  }
}

/// Orchestrates the restore purchases flow.
/// AC-11: Calls RevenueCat SDK restorePurchases() then calls backend POST /subscriptions/restore.
class RestorePurchasesController extends ChangeNotifier {
  RestorePurchasesController({
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

  bool _isRestoring = false;
  RestorePurchasesOutcome? _lastOutcome;

  bool get isRestoring => _isRestoring;
  RestorePurchasesOutcome? get lastOutcome => _lastOutcome;

  /// Executes the full restore purchases flow.
  Future<RestorePurchasesOutcome> restorePurchases() async {
    _isRestoring = true;
    _lastOutcome = null;
    notifyListeners();

    try {
      // Step 1: Call RevenueCat SDK to restore from app store
      final restoreResult = await _revenueCatSdk.restorePurchases();

      // Step 2: Call backend POST /subscriptions/restore
      final backendResult = await _gateway.restorePurchases(
        householdId: _householdId,
        revenueCatAppUserId: restoreResult.appUserId,
      );

      // Step 3: Invalidate entitlement cache to pick up new state
      _entitlementProvider.invalidateCache();
      await _entitlementProvider.fetchEntitlements(_householdId);

      _lastOutcome = RestorePurchasesOutcome.success(
        status: backendResult.status,
        tier: backendResult.tier,
      );
    } catch (e) {
      _lastOutcome = RestorePurchasesOutcome.failure(e.toString());
    } finally {
      _isRestoring = false;
      notifyListeners();
    }

    return _lastOutcome!;
  }
}
