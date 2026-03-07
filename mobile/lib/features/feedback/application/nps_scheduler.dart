/// Determines NPS prompt eligibility based on timing rules.
/// AC-12: First prompt after 7 days, then 30-day intervals.
class NpsScheduler {
  NpsScheduler({
    required DateTime Function() nowProvider,
  }) : _nowProvider = nowProvider;

  final DateTime Function() _nowProvider;

  DateTime? _firstLaunchDate;
  DateTime? _lastPromptDate;

  /// Records the app's first launch date.
  void recordFirstLaunch(DateTime date) {
    _firstLaunchDate = date;
  }

  /// Records the date when NPS was last prompted.
  void recordLastPrompt(DateTime date) {
    _lastPromptDate = date;
  }

  /// Returns the first launch date, if recorded.
  DateTime? get firstLaunchDate => _firstLaunchDate;

  /// Returns the last prompt date, if recorded.
  DateTime? get lastPromptDate => _lastPromptDate;

  /// Determines whether the user is eligible for an NPS prompt.
  bool isEligible() {
    final now = _nowProvider();

    if (_firstLaunchDate == null) {
      return false;
    }

    // First prompt: at least 7 days after first launch
    if (_lastPromptDate == null) {
      return now.difference(_firstLaunchDate!).inDays >= 7;
    }

    // Subsequent prompts: at least 30 days after last prompt
    return now.difference(_lastPromptDate!).inDays >= 30;
  }
}
