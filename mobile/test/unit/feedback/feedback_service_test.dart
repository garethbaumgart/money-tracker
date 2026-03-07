import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/feedback/application/feedback_service.dart';
import 'package:money_tracker/features/feedback/domain/feedback_category.dart';
import 'package:money_tracker/features/feedback/domain/feedback_submission.dart';
import 'package:money_tracker/features/feedback/domain/nps_prompt.dart';
import 'package:money_tracker/features/feedback/infrastructure/feedback_api_client.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({
    required this.statusCode,
    required this.responseBody,
  });

  final int statusCode;
  final String responseBody;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }
}

void main() {
  group('FeedbackService', () {
    late FeedbackService service;

    setUp(() {
      final httpClient = StubHttpClient(
        statusCode: 200,
        responseBody: jsonEncode({
          'feedbackId': '123e4567-e89b-12d3-a456-426614174000',
          'status': 'New',
        }),
      );
      final apiClient = FeedbackApiClient(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: httpClient,
      );
      service = FeedbackService(apiClient: apiClient);
    });

    test('submitFeedback returns success for valid submission', () async {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.bug,
        description: 'App crashes',
        rating: 3,
      );

      final result = await service.submitFeedback(submission);

      expect(result, isA<FeedbackSuccess>());
    });

    test('submitFeedback returns failure for invalid submission', () async {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.general,
        description: '',
        rating: 3,
      );

      final result = await service.submitFeedback(submission);

      expect(result, isA<FeedbackFailure>());
      expect(
        (result as FeedbackFailure).errorMessage,
        contains('Description is required'),
      );
    });

    test('submitNps returns success for valid score', () async {
      final npsHttpClient = StubHttpClient(
        statusCode: 200,
        responseBody: jsonEncode({
          'npsId': '223e4567-e89b-12d3-a456-426614174000',
        }),
      );
      final apiClient = FeedbackApiClient(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: npsHttpClient,
      );
      final npsService = FeedbackService(apiClient: apiClient);

      const prompt = NpsPrompt(score: 9, comment: 'Great!');

      final result = await npsService.submitNps(prompt);

      expect(result, isA<FeedbackSuccess>());
    });

    test('submitNps returns failure for invalid score', () async {
      const prompt = NpsPrompt(score: 11);

      final result = await service.submitNps(prompt);

      expect(result, isA<FeedbackFailure>());
    });

    test('submitFeedback returns failure when API fails', () async {
      final failHttpClient = StubHttpClient(
        statusCode: 500,
        responseBody: '{"error":"internal"}',
      );
      final apiClient = FeedbackApiClient(
        apiBaseUrl: Uri.parse('https://api.example.com'),
        tokenProvider: () => 'test-token',
        httpClient: failHttpClient,
      );
      final failService = FeedbackService(apiClient: apiClient);

      const submission = FeedbackSubmission(
        category: FeedbackCategory.bug,
        description: 'Some bug',
        rating: 3,
      );

      final result = await failService.submitFeedback(submission);

      expect(result, isA<FeedbackFailure>());
    });
  });
}
