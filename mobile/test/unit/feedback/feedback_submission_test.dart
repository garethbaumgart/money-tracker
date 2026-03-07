import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/feedback/domain/feedback_category.dart';
import 'package:money_tracker/features/feedback/domain/feedback_submission.dart';

void main() {
  group('FeedbackSubmission', () {
    test('validate returns null for valid submission', () {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.bug,
        description: 'The app crashes on startup',
        rating: 4,
      );

      expect(submission.validate(), isNull);
    });

    test('validate returns error for empty description', () {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.general,
        description: '',
        rating: 3,
      );

      expect(submission.validate(), isNotNull);
      expect(submission.validate(), contains('Description is required'));
    });

    test('validate returns error for description exceeding max length', () {
      final submission = FeedbackSubmission(
        category: FeedbackCategory.general,
        description: 'x' * 5001,
        rating: 3,
      );

      expect(submission.validate(), isNotNull);
      expect(submission.validate(), contains('maximum length'));
    });

    test('validate returns error for rating below 1', () {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.general,
        description: 'Some feedback',
        rating: 0,
      );

      expect(submission.validate(), isNotNull);
      expect(submission.validate(), contains('between 1 and 5'));
    });

    test('validate returns error for rating above 5', () {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.general,
        description: 'Some feedback',
        rating: 6,
      );

      expect(submission.validate(), isNotNull);
      expect(submission.validate(), contains('between 1 and 5'));
    });

    test('toJson produces correct output', () {
      const submission = FeedbackSubmission(
        category: FeedbackCategory.bug,
        description: 'App crashes',
        rating: 2,
        screenName: 'home',
        appVersion: '1.0.0',
      );

      final json = submission.toJson();

      expect(json['category'], 'Bug');
      expect(json['description'], 'App crashes');
      expect(json['rating'], 2);
      expect(json['screenName'], 'home');
      expect(json['appVersion'], '1.0.0');
      expect(json.containsKey('deviceModel'), isFalse);
    });
  });

  group('FeedbackCategory', () {
    test('fromString parses bug', () {
      expect(FeedbackCategory.fromString('bug'), FeedbackCategory.bug);
    });

    test('fromString parses feature case-insensitively', () {
      expect(FeedbackCategory.fromString('Feature'), FeedbackCategory.feature);
    });

    test('fromString defaults to general for unknown', () {
      expect(FeedbackCategory.fromString('unknown'), FeedbackCategory.general);
    });

    test('toApiString returns capitalized name', () {
      expect(FeedbackCategory.bug.toApiString(), 'Bug');
      expect(FeedbackCategory.feature.toApiString(), 'Feature');
      expect(FeedbackCategory.general.toApiString(), 'General');
    });
  });
}
