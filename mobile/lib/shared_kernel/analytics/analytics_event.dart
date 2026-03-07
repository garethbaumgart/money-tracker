/// A structured analytics event representing a user activation milestone.
class AnalyticsEvent {
  AnalyticsEvent({
    required this.milestone,
    this.householdId,
    this.metadata,
    required this.occurredAtUtc,
  });

  /// The milestone identifier (e.g. "signup_completed", "household_created").
  final String milestone;

  /// Optional household context for household-scoped milestones.
  final String? householdId;

  /// Optional key-value metadata for enrichment.
  final Map<String, String>? metadata;

  /// The UTC timestamp when the milestone occurred on the client.
  final DateTime occurredAtUtc;

  Map<String, dynamic> toJson() => {
        'milestone': milestone,
        if (householdId != null) 'householdId': householdId,
        if (metadata != null) 'metadata': metadata,
        'occurredAtUtc': occurredAtUtc.toUtc().toIso8601String(),
      };

  factory AnalyticsEvent.fromJson(Map<String, dynamic> json) {
    return AnalyticsEvent(
      milestone: json['milestone'] as String,
      householdId: json['householdId'] as String?,
      metadata: json['metadata'] != null
          ? Map<String, String>.from(json['metadata'] as Map)
          : null,
      occurredAtUtc: DateTime.parse(json['occurredAtUtc'] as String),
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is AnalyticsEvent &&
          runtimeType == other.runtimeType &&
          milestone == other.milestone &&
          householdId == other.householdId &&
          occurredAtUtc == other.occurredAtUtc;

  @override
  int get hashCode =>
      milestone.hashCode ^ householdId.hashCode ^ occurredAtUtc.hashCode;

  @override
  String toString() =>
      'AnalyticsEvent(milestone: $milestone, householdId: $householdId, '
      'occurredAtUtc: $occurredAtUtc)';
}
