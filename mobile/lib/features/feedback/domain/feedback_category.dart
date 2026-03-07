enum FeedbackCategory {
  bug,
  feature,
  general;

  static FeedbackCategory fromString(String value) {
    return FeedbackCategory.values.firstWhere(
      (category) => category.name.toLowerCase() == value.toLowerCase(),
      orElse: () => FeedbackCategory.general,
    );
  }

  String toApiString() => name[0].toUpperCase() + name.substring(1);
}
