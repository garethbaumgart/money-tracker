import 'feedback_category.dart';

class FeedbackSubmission {
  const FeedbackSubmission({
    required this.category,
    required this.description,
    required this.rating,
    this.screenName,
    this.appVersion,
    this.deviceModel,
    this.osVersion,
  });

  final FeedbackCategory category;
  final String description;
  final int rating;
  final String? screenName;
  final String? appVersion;
  final String? deviceModel;
  final String? osVersion;

  /// Minimum description length.
  static const int minDescriptionLength = 10;

  /// Maximum description length.
  static const int maxDescriptionLength = 5000;

  /// Validates the submission fields.
  /// Returns null if valid, or an error message string if invalid.
  String? validate() {
    if (description.trim().isEmpty) {
      return 'Description is required.';
    }
    if (description.trim().length < minDescriptionLength) {
      return 'Description must be at least $minDescriptionLength characters.';
    }
    if (description.length > maxDescriptionLength) {
      return 'Description exceeds maximum length of $maxDescriptionLength characters.';
    }
    if (rating < 1 || rating > 5) {
      return 'Rating must be between 1 and 5.';
    }
    return null;
  }

  Map<String, dynamic> toJson() {
    return {
      'category': category.toApiString(),
      'description': description.trim(),
      'rating': rating,
      if (screenName != null) 'screenName': screenName,
      if (appVersion != null) 'appVersion': appVersion,
      if (deviceModel != null) 'deviceModel': deviceModel,
      if (osVersion != null) 'osVersion': osVersion,
    };
  }
}
