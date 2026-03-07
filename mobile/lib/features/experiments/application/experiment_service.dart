import 'package:flutter/foundation.dart';
import '../domain/experiment_allocation.dart';
import '../infrastructure/experiment_api_client.dart';

class ExperimentService extends ChangeNotifier {
  ExperimentService({
    required ExperimentApiClient apiClient,
    Duration cacheTtl = const Duration(minutes: 5),
  })  : _apiClient = apiClient,
        _cacheTtl = cacheTtl;

  final ExperimentApiClient _apiClient;
  final Duration _cacheTtl;

  List<ExperimentAllocation> _allocations = [];
  DateTime? _lastFetchedAt;
  bool _isLoading = false;

  List<ExperimentAllocation> get allocations => _allocations;
  bool get isLoading => _isLoading;

  bool get _isCacheValid {
    if (_lastFetchedAt == null) return false;
    return DateTime.now().difference(_lastFetchedAt!) < _cacheTtl;
  }

  String? getVariant(String experimentName) {
    final allocation = _allocations.cast<ExperimentAllocation?>().firstWhere(
          (a) => a!.experimentName == experimentName,
          orElse: () => null,
        );
    return allocation?.variantName;
  }

  Future<List<ExperimentAllocation>> fetchAllocations() async {
    if (_isCacheValid) {
      return _allocations;
    }

    _isLoading = true;
    notifyListeners();

    try {
      final result = await _apiClient.getActiveAllocations();
      _allocations = result;
      _lastFetchedAt = DateTime.now();
    } catch (e) {
      // On failure, keep existing allocations as fallback
      debugPrint('ExperimentService: Failed to fetch allocations: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }

    return _allocations;
  }

  void invalidateCache() {
    _lastFetchedAt = null;
  }
}
