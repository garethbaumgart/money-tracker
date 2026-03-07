import 'package:flutter/foundation.dart';
import '../application/offerings_provider.dart';
import '../application/purchase_controller.dart';
import '../domain/offering.dart';
import '../domain/purchase_result.dart';

/// State of the paywall screen.
enum PaywallState {
  loading,
  loaded,
  purchasing,
  success,
  error,
  empty,
}

/// Manages paywall screen state.
/// Coordinates between OfferingsProvider (data) and PurchaseController (actions).
class PaywallController extends ChangeNotifier {
  PaywallController({
    required OfferingsProvider offeringsProvider,
    required PurchaseController purchaseController,
    this.source,
    this.variant = 'A',
  })  : _offeringsProvider = offeringsProvider,
        _purchaseController = purchaseController;

  final OfferingsProvider _offeringsProvider;
  final PurchaseController _purchaseController;

  /// Source that triggered the paywall (e.g. "feature_gate", "settings").
  final String? source;

  /// A/B test variant identifier. AC-13: Paywall supports variant parameter.
  final String variant;

  PaywallState _state = PaywallState.loading;
  Package? _selectedPackage;
  String? _errorMessage;

  /// Current state of the paywall.
  PaywallState get state => _state;

  /// The current offering.
  Offering? get offering => _offeringsProvider.offering;

  /// The currently selected package.
  Package? get selectedPackage => _selectedPackage;

  /// Error message, if any.
  String? get errorMessage => _errorMessage;

  /// Whether a purchase is in progress.
  bool get isPurchasing => _state == PaywallState.purchasing;

  /// Available packages from the current offering.
  List<Package> get packages => offering?.packages ?? const [];

  /// The annual package, if available.
  Package? get annualPackage => offering?.annual;

  /// The monthly package, if available.
  Package? get monthlyPackage => offering?.monthly;

  /// Loads offerings and initializes the paywall.
  /// Selects annual plan by default (annual-first presentation).
  Future<void> loadOfferings() async {
    _state = PaywallState.loading;
    _errorMessage = null;
    notifyListeners();

    try {
      await _offeringsProvider.fetchOffering();

      if (offering == null || packages.isEmpty) {
        _state = PaywallState.empty;
        _errorMessage = 'No plans are currently available. Please try again later.';
      } else {
        _state = PaywallState.loaded;
        // AC-1: Annual plan is primary (selected by default).
        _selectedPackage = annualPackage ?? packages.first;
      }
    } catch (e) {
      _state = PaywallState.error;
      _errorMessage = e.toString();
    }

    notifyListeners();
  }

  /// Selects a package for purchase.
  void selectPackage(Package package) {
    _selectedPackage = package;
    notifyListeners();
  }

  /// Initiates the purchase for the selected package.
  Future<PurchaseResult?> purchaseSelectedPackage() async {
    if (_selectedPackage == null) return null;

    _state = PaywallState.purchasing;
    _errorMessage = null;
    notifyListeners();

    final result = await _purchaseController.purchase(_selectedPackage!);

    switch (result) {
      case PurchaseSuccess():
        _state = PaywallState.success;
      case PurchaseCancelled():
        // AC-7: Return to loaded state without error.
        _state = PaywallState.loaded;
      case PurchasePending():
        _state = PaywallState.loaded;
      case PurchaseFailed(:final errorMessage):
        _state = PaywallState.error;
        _errorMessage = errorMessage;
    }

    notifyListeners();
    return result;
  }

  /// Resets to loaded state for retry.
  void resetError() {
    if (offering != null && packages.isNotEmpty) {
      _state = PaywallState.loaded;
    } else {
      _state = PaywallState.empty;
    }
    _errorMessage = null;
    notifyListeners();
  }
}
