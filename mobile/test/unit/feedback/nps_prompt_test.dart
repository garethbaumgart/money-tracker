import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/feedback/domain/nps_prompt.dart';

void main() {
  group('NpsPrompt', () {
    test('validate returns null for valid score', () {
      const prompt = NpsPrompt(score: 8);

      expect(prompt.validate(), isNull);
    });

    test('validate returns null for score 0', () {
      const prompt = NpsPrompt(score: 0);

      expect(prompt.validate(), isNull);
    });

    test('validate returns null for score 10', () {
      const prompt = NpsPrompt(score: 10);

      expect(prompt.validate(), isNull);
    });

    test('validate returns error for negative score', () {
      const prompt = NpsPrompt(score: -1);

      expect(prompt.validate(), isNotNull);
      expect(prompt.validate(), contains('between 0 and 10'));
    });

    test('validate returns error for score above 10', () {
      const prompt = NpsPrompt(score: 11);

      expect(prompt.validate(), isNotNull);
      expect(prompt.validate(), contains('between 0 and 10'));
    });

    test('validate returns error for comment exceeding max length', () {
      final prompt = NpsPrompt(score: 8, comment: 'x' * 1001);

      expect(prompt.validate(), isNotNull);
      expect(prompt.validate(), contains('maximum length'));
    });

    test('toJson produces correct output with comment', () {
      const prompt = NpsPrompt(score: 9, comment: 'Great app!');

      final json = prompt.toJson();

      expect(json['score'], 9);
      expect(json['comment'], 'Great app!');
    });

    test('toJson omits null comment', () {
      const prompt = NpsPrompt(score: 5);

      final json = prompt.toJson();

      expect(json['score'], 5);
      expect(json.containsKey('comment'), isFalse);
    });
  });
}
