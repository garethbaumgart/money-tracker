import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../../subscriptions/infrastructure/subscription_gateway.dart';
import '../domain/feedback_submission.dart';
import '../domain/nps_prompt.dart';

/// API client for feedback endpoints.
class FeedbackApiClient {
  FeedbackApiClient({
    required Uri apiBaseUrl,
    required String Function() tokenProvider,
    @visibleForTesting HttpClientAdapter? httpClient,
  })  : _apiBaseUrl = apiBaseUrl,
        _tokenProvider = tokenProvider,
        _httpClient = httpClient;

  final Uri _apiBaseUrl;
  final String Function() _tokenProvider;
  final HttpClientAdapter? _httpClient;

  /// Submits feedback and returns the feedback ID.
  Future<String> submitFeedback(FeedbackSubmission submission) async {
    final uri = _apiBaseUrl.replace(path: '/feedback');
    final body = jsonEncode(submission.toJson());
    final response = await _performPost(uri, body);

    if (response.statusCode != 200) {
      throw FeedbackApiException(
        'Failed to submit feedback: ${response.statusCode}',
      );
    }

    final responseBody = jsonDecode(response.body) as Map<String, dynamic>;
    return responseBody['feedbackId'] as String;
  }

  /// Submits an NPS score and returns the NPS ID.
  Future<String> submitNps(NpsPrompt prompt) async {
    final uri = _apiBaseUrl.replace(path: '/feedback/nps');
    final body = jsonEncode(prompt.toJson());
    final response = await _performPost(uri, body);

    if (response.statusCode != 200) {
      throw FeedbackApiException(
        'Failed to submit NPS: ${response.statusCode}',
      );
    }

    final responseBody = jsonDecode(response.body) as Map<String, dynamic>;
    return responseBody['npsId'] as String;
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

class FeedbackApiException implements Exception {
  FeedbackApiException(this.message);

  final String message;

  @override
  String toString() => 'FeedbackApiException: $message';
}
