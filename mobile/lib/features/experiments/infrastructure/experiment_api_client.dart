import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../../subscriptions/infrastructure/subscription_gateway.dart';
import '../domain/experiment_allocation.dart';

class ExperimentApiClient {
  ExperimentApiClient({
    required Uri apiBaseUrl,
    required String Function() tokenProvider,
    @visibleForTesting HttpClientAdapter? httpClient,
  })  : _apiBaseUrl = apiBaseUrl,
        _tokenProvider = tokenProvider,
        _httpClient = httpClient;

  final Uri _apiBaseUrl;
  final String Function() _tokenProvider;
  final HttpClientAdapter? _httpClient;

  Future<List<ExperimentAllocation>> getActiveAllocations() async {
    final uri = _apiBaseUrl.replace(path: '/experiments/active');

    final response = await _performGet(uri);

    if (response.statusCode != 200) {
      throw ExperimentApiException(
        'Failed to fetch active allocations: ${response.statusCode}',
      );
    }

    final body = jsonDecode(response.body) as Map<String, dynamic>;
    final allocations = (body['allocations'] as List<dynamic>)
        .map((e) => ExperimentAllocation.fromJson(e as Map<String, dynamic>))
        .toList();

    return allocations;
  }

  Future<ExperimentAllocation> allocateUser(String experimentId) async {
    final uri = _apiBaseUrl.replace(path: '/experiments/allocate');

    final requestBody = jsonEncode({
      'experimentId': experimentId,
    });

    final response = await _performPost(uri, requestBody);

    if (response.statusCode != 200) {
      throw ExperimentApiException(
        'Failed to allocate user: ${response.statusCode}',
      );
    }

    final body = jsonDecode(response.body) as Map<String, dynamic>;
    return ExperimentAllocation.fromJson(body);
  }

  Future<void> recordConversion(String experimentId) async {
    final uri = _apiBaseUrl.replace(path: '/experiments/convert');

    final requestBody = jsonEncode({
      'experimentId': experimentId,
    });

    final response = await _performPost(uri, requestBody);

    if (response.statusCode != 204) {
      throw ExperimentApiException(
        'Failed to record conversion: ${response.statusCode}',
      );
    }
  }

  Future<HttpResponse> _performGet(Uri uri) async {
    if (_httpClient != null) {
      return _httpClient!.get(uri, headers: _buildHeaders());
    }

    throw UnimplementedError(
      'HTTP client not configured. Provide an HttpClientAdapter.',
    );
  }

  Future<HttpResponse> _performPost(Uri uri, String body) async {
    if (_httpClient != null) {
      return _httpClient!.post(uri, body: body, headers: _buildHeaders());
    }

    throw UnimplementedError(
      'HTTP client not configured. Provide an HttpClientAdapter.',
    );
  }

  Map<String, String> _buildHeaders() {
    return {
      'Authorization': 'Bearer ${_tokenProvider()}',
      'Content-Type': 'application/json',
    };
  }
}

class ExperimentApiException implements Exception {
  ExperimentApiException(this.message);

  final String message;

  @override
  String toString() => 'ExperimentApiException: $message';
}
