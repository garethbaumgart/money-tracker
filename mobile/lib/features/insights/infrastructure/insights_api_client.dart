import 'dart:convert';
import 'package:flutter/foundation.dart';

import '../domain/spending_analysis.dart';
import '../domain/budget_health.dart';

/// HTTP client for insights endpoints.
///
/// In V1 this is a stub; when the API backend is wired the methods
/// will use a real HTTP client to call the server.
class InsightsApiClient {
  InsightsApiClient({this.baseUrl = 'http://localhost:5000'});

  final String baseUrl;

  /// Fetches spending summary for a household and period.
  ///
  /// Returns null when the response indicates a premium-required error.
  Future<SpendingAnalysis?> getSpendingSummary({
    required String householdId,
    required String period,
    required String bearerToken,
  }) async {
    // TODO: Replace with real HTTP call when wired.
    debugPrint(
      'GET $baseUrl/insights/spending-summary?householdId=$householdId&period=$period',
    );
    return null;
  }

  /// Fetches budget health score for a household.
  ///
  /// Returns null when the response indicates a premium-required error.
  Future<BudgetHealth?> getBudgetHealth({
    required String householdId,
    required String bearerToken,
  }) async {
    // TODO: Replace with real HTTP call when wired.
    debugPrint(
      'GET $baseUrl/insights/budget-health?householdId=$householdId',
    );
    return null;
  }

  /// Parses a spending summary response body.
  static SpendingAnalysis parseSpendingSummary(String responseBody) {
    final json = jsonDecode(responseBody) as Map<String, dynamic>;
    return SpendingAnalysis.fromJson(json);
  }

  /// Parses a budget health response body.
  static BudgetHealth parseBudgetHealth(String responseBody) {
    final json = jsonDecode(responseBody) as Map<String, dynamic>;
    return BudgetHealth.fromJson(json);
  }
}
