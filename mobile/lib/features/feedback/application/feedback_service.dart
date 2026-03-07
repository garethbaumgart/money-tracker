import '../domain/feedback_submission.dart';
import '../domain/nps_prompt.dart';
import '../infrastructure/feedback_api_client.dart';

/// Result of a feedback or NPS submission.
sealed class FeedbackResult {}

class FeedbackSuccess extends FeedbackResult {
  FeedbackSuccess({required this.id});

  final String id;
}

class FeedbackFailure extends FeedbackResult {
  FeedbackFailure({required this.errorMessage});

  final String errorMessage;
}

/// Application service for submitting feedback and NPS scores.
class FeedbackService {
  FeedbackService({
    required FeedbackApiClient apiClient,
  }) : _apiClient = apiClient;

  final FeedbackApiClient _apiClient;

  /// Submits user feedback to the backend.
  Future<FeedbackResult> submitFeedback(FeedbackSubmission submission) async {
    final validationError = submission.validate();
    if (validationError != null) {
      return FeedbackFailure(errorMessage: validationError);
    }

    try {
      final id = await _apiClient.submitFeedback(submission);
      return FeedbackSuccess(id: id);
    } catch (e) {
      return FeedbackFailure(errorMessage: e.toString());
    }
  }

  /// Submits an NPS score to the backend.
  Future<FeedbackResult> submitNps(NpsPrompt prompt) async {
    final validationError = prompt.validate();
    if (validationError != null) {
      return FeedbackFailure(errorMessage: validationError);
    }

    try {
      final id = await _apiClient.submitNps(prompt);
      return FeedbackSuccess(id: id);
    } catch (e) {
      return FeedbackFailure(errorMessage: e.toString());
    }
  }
}
