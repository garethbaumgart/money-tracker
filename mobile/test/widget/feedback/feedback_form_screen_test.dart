import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/money_tracker_theme.dart';
import 'package:money_tracker/features/feedback/application/feedback_service.dart';
import 'package:money_tracker/features/feedback/infrastructure/feedback_api_client.dart';
import 'package:money_tracker/features/feedback/presentation/feedback_form_screen.dart';
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

FeedbackService _createService({int statusCode = 200}) {
  final httpClient = StubHttpClient(
    statusCode: statusCode,
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
  return FeedbackService(apiClient: apiClient);
}

Widget _buildTestApp({required FeedbackService feedbackService}) {
  return MaterialApp(
    theme: MoneyTrackerTheme.light(),
    home: FeedbackFormScreen(feedbackService: feedbackService),
  );
}

void main() {
  group('FeedbackFormScreen validation', () {
    testWidgets('submit button is disabled when description is empty',
        (tester) async {
      final service = _createService();

      await tester.pumpWidget(_buildTestApp(feedbackService: service));
      await tester.pumpAndSettle();

      // Submit button should be disabled when description is empty.
      final submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNull);

      // No error text should be visible initially.
      expect(find.text('Please enter a description'), findsNothing);
    });

    testWidgets(
        'submit button is disabled when description is too short',
        (tester) async {
      final service = _createService();

      await tester.pumpWidget(_buildTestApp(feedbackService: service));
      await tester.pumpAndSettle();

      // Enter a short description (less than 10 chars).
      await tester.enterText(find.byType(TextFormField), 'Short');
      await tester.pumpAndSettle();

      // Button should still be disabled (5 chars < 10 min).
      final submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNull);
    });

    testWidgets(
        'submit button is enabled when description meets minimum length',
        (tester) async {
      final service = _createService();

      await tester.pumpWidget(_buildTestApp(feedbackService: service));
      await tester.pumpAndSettle();

      // Enter a valid description (>= 10 chars).
      await tester.enterText(
        find.byType(TextFormField),
        'This is valid feedback text',
      );
      await tester.pumpAndSettle();

      final submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNotNull);
    });

    testWidgets(
        'submit button transitions from disabled to enabled as user types',
        (tester) async {
      final service = _createService();

      await tester.pumpWidget(_buildTestApp(feedbackService: service));
      await tester.pumpAndSettle();

      // Initially disabled.
      var submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNull);

      // Type 9 characters -- still disabled.
      await tester.enterText(find.byType(TextFormField), '123456789');
      await tester.pumpAndSettle();

      submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNull);

      // Type 10 characters -- now enabled.
      await tester.enterText(find.byType(TextFormField), '1234567890');
      await tester.pumpAndSettle();

      submitButton = tester.widget<FilledButton>(
        find.byType(FilledButton),
      );
      expect(submitButton.onPressed, isNotNull);
    });

    testWidgets('successful submission pops the screen and shows snackbar',
        (tester) async {
      final service = _createService();

      await tester.pumpWidget(
        MaterialApp(
          theme: MoneyTrackerTheme.light(),
          home: Builder(
            builder: (context) => Scaffold(
              body: ElevatedButton(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) =>
                          FeedbackFormScreen(feedbackService: service),
                    ),
                  );
                },
                child: const Text('Open Feedback'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Navigate to the feedback form.
      await tester.tap(find.text('Open Feedback'));
      await tester.pumpAndSettle();

      // Enter a valid description.
      await tester.enterText(
        find.byType(TextFormField),
        'This is valid feedback text for testing',
      );
      await tester.pumpAndSettle();

      // Tap the submit button.
      await tester.tap(find.byType(FilledButton));
      await tester.pumpAndSettle();

      // Should show a snackbar with success message.
      expect(find.text('Thank you for your feedback!'), findsOneWidget);

      // Should have popped back to the previous screen.
      expect(find.text('Open Feedback'), findsOneWidget);
      expect(find.byType(FeedbackFormScreen), findsNothing);
    });
  });
}

