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

  Future<RestoreResponse> restorePurchases({
    required String householdId,
    required String revenueCatAppUserId,
  }) async {
    final uri = _apiBaseUrl.replace(path: '/subscriptions/restore');

    final body = jsonEncode({
      'householdId': householdId,
      'revenueCatAppUserId': revenueCatAppUserId,
    });

    final response = await _performPost(uri, body);

    if (response.statusCode != 200) {
      throw SubscriptionGatewayException(
        'Failed to restore purchases: ${response.statusCode}',
      );
    }

    final responseBody = jsonDecode(response.body) as Map<String, dynamic>;

    return RestoreResponse(
      status: responseBody['status'] as String,
      tier: responseBody['tier'] as String,
      featureKeys: (responseBody['featureKeys'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
      currentPeriodEndUtc: responseBody['currentPeriodEndUtc'] != null
          ? DateTime.parse(responseBody['currentPeriodEndUtc'] as String)
          : null,
    );
  }

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
      return _httpClient.get(uri, headers: _buildHeaders());
    }

    // Default implementation would use dart:io HttpClient
    // For now, throw if no adapter is provided (will be wired in app bootstrap)
    throw UnimplementedError(
      'HTTP client not configured. Provide an HttpClientAdapter.',
    );
  }

  Future<HttpResponse> _performPost(Uri uri, String body) async {
    if (_httpClient != null) {
      return _httpClient.post(uri, body: body, headers: _buildHeaders());
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

class RestoreResponse {
  const RestoreResponse({
    required this.status,
    required this.tier,
    required this.featureKeys,
    this.currentPeriodEndUtc,
  });

  final String status;
  final String tier;
  final List<String> featureKeys;
  final DateTime? currentPeriodEndUtc;
}

abstract class HttpClientAdapter {
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers});
  Future<HttpResponse> post(Uri uri, {String? body, Map<String, String>? headers});
}
