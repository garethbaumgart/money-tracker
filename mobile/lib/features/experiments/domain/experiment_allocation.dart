class ExperimentAllocation {
  const ExperimentAllocation({
    required this.experimentId,
    required this.experimentName,
    required this.variantName,
    required this.allocatedAtUtc,
  });

  final String experimentId;
  final String experimentName;
  final String variantName;
  final DateTime allocatedAtUtc;

  factory ExperimentAllocation.fromJson(Map<String, dynamic> json) {
    return ExperimentAllocation(
      experimentId: json['experimentId'] as String,
      experimentName: json['experimentName'] as String,
      variantName: json['variantName'] as String,
      allocatedAtUtc: DateTime.parse(json['allocatedAtUtc'] as String),
    );
  }
}
