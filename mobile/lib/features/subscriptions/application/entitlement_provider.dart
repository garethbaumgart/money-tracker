import 'package:flutter/foundation.dart';
import '../domain/entitlement_set.dart';
import '../domain/feature_key.dart';
import '../infrastructure/subscription_gateway.dart';

class EntitlementProvider extends ChangeNotifier {
  EntitlementProvider({
    required SubscriptionGateway gateway,
    Duration cacheTtl = const Duration(minutes: 5),
  })  : _gateway = gateway,
        _cacheTtl = cacheTtl;

  final SubscriptionGateway _gateway;
  final Duration _cacheTtl;

  EntitlementSet _entitlements = EntitlementSet.free();
  DateTime? _lastFetchedAt;
  bool _isLoading = false;

  EntitlementSet get entitlements => _entitlements;
  bool get isLoading => _isLoading;

  bool get _isCacheValid {
    if (_lastFetchedAt == null) return false;
    return DateTime.now().difference(_lastFetchedAt!) < _cacheTtl;
  }

  bool hasFeature(FeatureKey feature) => _entitlements.hasFeature(feature);

  Future<EntitlementSet> fetchEntitlements(String householdId) async {
    if (_isCacheValid) {
      return _entitlements;
    }

    _isLoading = true;
    notifyListeners();

    try {
      final result = await _gateway.getEntitlements(householdId);
      _entitlements = result;
      _lastFetchedAt = DateTime.now();
    } finally {
      _isLoading = false;
      notifyListeners();
    }

    return _entitlements;
  }

  void invalidateCache() {
    _lastFetchedAt = null;
  }
}
