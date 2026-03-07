class NpsPrompt {
  const NpsPrompt({
    required this.score,
    this.comment,
  });

  final int score;
  final String? comment;

  /// Maximum comment length.
  static const int maxCommentLength = 1000;

  /// Validates the NPS submission.
  /// Returns null if valid, or an error message string if invalid.
  String? validate() {
    if (score < 0 || score > 10) {
      return 'Score must be between 0 and 10.';
    }
    if (comment != null && comment!.length > maxCommentLength) {
      return 'Comment exceeds maximum length of $maxCommentLength characters.';
    }
    return null;
  }

  Map<String, dynamic> toJson() {
    return {
      'score': score,
      if (comment != null) 'comment': comment!.trim(),
    };
  }
}
