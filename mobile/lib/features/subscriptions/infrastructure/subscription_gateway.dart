import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../domain/entitlement_set.dart';

class SubscriptionGateway {
  SubscriptionGateway({
    required Uri apiBaseUrl,
    required String Function() tokenProvider,
    @visibleForTesting HttpClientAdapter? httpClient,
  })  : _apiBaseUrl = apiBaseUrl,
        _tokenProvider = tokenProvider,
        _httpClient = httpClient;

  final Uri _apiBaseUrl;
  final String Function() _tokenProvider;
  final HttpClientAdapter? _httpClient;

  Future<EntitlementSet> getEntitlements(String householdId) async {
    final uri = _apiBaseUrl.replace(
      path: '/subscriptions/entitlements',
      queryParameters: {'householdId': householdId},
    );

    final response = await _performGet(uri);

    if (response.statusCode != 200) {
      throw SubscriptionGatewayException(
        'Failed to fetch entitlements: ${response.statusCode}',
      );
    }

    final body = jsonDecode(response.body) as Map<String, dynamic>;

    final tier = body['tier'] as String;
    final featureKeys = (body['featureKeys'] as List<dynamic>)
        .map((e) => e as String)
        .toList();
    final trialExpiresAtUtc = body['trialExpiresAtUtc'] != null
        ? DateTime.parse(body['trialExpiresAtUtc'] as String)
        : null;
    final currentPeriodEndUtc = body['currentPeriodEndUtc'] != null
        ? DateTime.parse(body['currentPeriodEndUtc'] as String)
        : null;

    return EntitlementSet.fromApiResponse(
      tier: tier,
      featureKeys: featureKeys,
      trialExpiresAtUtc: trialExpiresAtUtc,
      currentPeriodEndUtc: currentPeriodEndUtc,
    );
  }

  Future<HttpResponse> _performGet(Uri uri) async {
    if (_httpClient != null) {
      return _httpClient!.get(uri, headers: _buildHeaders());
    }

    // Default implementation would use dart:io HttpClient
    // For now, throw if no adapter is provided (will be wired in app bootstrap)
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

class SubscriptionGatewayException implements Exception {
  SubscriptionGatewayException(this.message);

  final String message;

  @override
  String toString() => 'SubscriptionGatewayException: $message';
}

class HttpResponse {
  const HttpResponse({required this.statusCode, required this.body});

  final int statusCode;
  final String body;
}

abstract class HttpClientAdapter {
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers});
}
