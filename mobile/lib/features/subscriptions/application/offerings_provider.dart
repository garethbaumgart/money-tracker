import 'package:flutter/foundation.dart';
import '../domain/offering.dart';
import '../infrastructure/revenuecat_sdk_adapter.dart';

/// Fetches and caches RevenueCat offerings.
/// AC-12: Caches offerings and serves from cache on subsequent access.
class OfferingsProvider extends ChangeNotifier {
  OfferingsProvider({
    required RevenueCatSdkAdapter revenueCatSdk,
    Duration cacheTtl = const Duration(minutes: 15),
  })  : _revenueCatSdk = revenueCatSdk,
        _cacheTtl = cacheTtl;

  final RevenueCatSdkAdapter _revenueCatSdk;
  final Duration _cacheTtl;

  Offering? _cachedOffering;
  DateTime? _lastFetchedAt;
  bool _isLoading = false;
  String? _errorMessage;

  /// The current cached offering, or null if not yet fetched.
  Offering? get offering => _cachedOffering;

  /// Whether offerings are currently being loaded.
  bool get isLoading => _isLoading;

  /// Error message from the last fetch attempt, if any.
  String? get errorMessage => _errorMessage;

  /// Whether the cache is still valid.
  bool get _isCacheValid {
    if (_lastFetchedAt == null || _cachedOffering == null) return false;
    return DateTime.now().difference(_lastFetchedAt!) < _cacheTtl;
  }

  /// Fetches the current offering from RevenueCat.
  /// Returns cached data if the cache is still valid.
  Future<Offering?> fetchOffering({bool forceRefresh = false}) async {
    if (_isCacheValid && !forceRefresh) {
      return _cachedOffering;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final offering = await _revenueCatSdk.getCurrentOffering();
      _cachedOffering = offering;
      _lastFetchedAt = DateTime.now();
    } catch (e) {
      _errorMessage = e.toString();
      debugPrint('OfferingsProvider: failed to fetch offerings: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }

    return _cachedOffering;
  }

  /// Invalidates the cache, forcing a fresh fetch on next access.
  void invalidateCache() {
    _lastFetchedAt = null;
  }
}
