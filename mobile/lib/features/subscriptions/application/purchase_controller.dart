import 'package:flutter/foundation.dart';
import '../domain/offering.dart';
import '../domain/purchase_result.dart';
import '../infrastructure/revenuecat_sdk_adapter.dart';
import 'entitlement_provider.dart';

/// State of the purchase flow.
enum PurchaseFlowState {
  idle,
  purchasing,
  success,
  cancelled,
  pending,
  error,
}

/// Orchestrates the purchase flow:
/// select plan -> RevenueCat purchase -> update entitlements.
class PurchaseController extends ChangeNotifier {
  PurchaseController({
    required RevenueCatSdkAdapter revenueCatSdk,
    required EntitlementProvider entitlementProvider,
    required String householdId,
  })  : _revenueCatSdk = revenueCatSdk,
        _entitlementProvider = entitlementProvider,
        _householdId = householdId;

  final RevenueCatSdkAdapter _revenueCatSdk;
  final EntitlementProvider _entitlementProvider;
  final String _householdId;

  PurchaseFlowState _state = PurchaseFlowState.idle;
  PurchaseResult? _lastResult;
  String? _errorMessage;

  /// Current state of the purchase flow.
  PurchaseFlowState get state => _state;

  /// The result of the last purchase attempt.
  PurchaseResult? get lastResult => _lastResult;

  /// Error message from the last failed purchase.
  String? get errorMessage => _errorMessage;

  /// Whether a purchase is currently in progress.
  bool get isPurchasing => _state == PurchaseFlowState.purchasing;

  /// Initiates a purchase for the given package.
  /// AC-4: Tapping "Subscribe" initiates RevenueCat purchase flow.
  /// AC-5: Successful purchase updates entitlements.
  /// AC-6: Failed purchase returns error.
  /// AC-7: Cancellation returns to idle without error.
  /// AC-8: Pending purchase shows pending message.
  Future<PurchaseResult> purchase(Package package) async {
    _state = PurchaseFlowState.purchasing;
    _lastResult = null;
    _errorMessage = null;
    notifyListeners();

    try {
      final result = await _revenueCatSdk.purchasePackage(package);
      _lastResult = result;

      switch (result) {
        case PurchaseSuccess():
          _state = PurchaseFlowState.success;
          // Invalidate and refresh entitlements after successful purchase.
          _entitlementProvider.invalidateCache();
          await _entitlementProvider.fetchEntitlements(_householdId);
        case PurchaseCancelled():
          _state = PurchaseFlowState.cancelled;
        case PurchasePending():
          _state = PurchaseFlowState.pending;
        case PurchaseFailed(:final errorMessage):
          _state = PurchaseFlowState.error;
          _errorMessage = errorMessage;
      }
    } catch (e) {
      _state = PurchaseFlowState.error;
      _errorMessage = e.toString();
      _lastResult = PurchaseFailed(
        errorMessage: e.toString(),
        isRetryable: true,
      );
    } finally {
      notifyListeners();
    }

    return _lastResult!;
  }

  /// Resets the controller to idle state.
  void reset() {
    _state = PurchaseFlowState.idle;
    _lastResult = null;
    _errorMessage = null;
    notifyListeners();
  }
}
