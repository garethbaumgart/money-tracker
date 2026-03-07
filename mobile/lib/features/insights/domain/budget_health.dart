class ScoreBreakdown {
  const ScoreBreakdown({
    required this.adherenceScore,
    required this.adherenceWeight,
    required this.velocityScore,
    required this.velocityWeight,
    required this.billPaymentScore,
    required this.billPaymentWeight,
  });

  final double adherenceScore;
  final double adherenceWeight;
  final double velocityScore;
  final double velocityWeight;
  final double billPaymentScore;
  final double billPaymentWeight;

  factory ScoreBreakdown.fromJson(Map<String, dynamic> json) {
    return ScoreBreakdown(
      adherenceScore: (json['adherenceScore'] as num).toDouble(),
      adherenceWeight: (json['adherenceWeight'] as num).toDouble(),
      velocityScore: (json['velocityScore'] as num).toDouble(),
      velocityWeight: (json['velocityWeight'] as num).toDouble(),
      billPaymentScore: (json['billPaymentScore'] as num).toDouble(),
      billPaymentWeight: (json['billPaymentWeight'] as num).toDouble(),
    );
  }
}

class CategoryHealth {
  const CategoryHealth({
    required this.categoryId,
    required this.categoryName,
    required this.allocated,
    required this.spent,
    required this.status,
  });

  final String categoryId;
  final String categoryName;
  final double allocated;
  final double spent;
  final String status;

  factory CategoryHealth.fromJson(Map<String, dynamic> json) {
    return CategoryHealth(
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      allocated: (json['allocated'] as num).toDouble(),
      spent: (json['spent'] as num).toDouble(),
      status: json['status'] as String,
    );
  }
}

class BudgetHealth {
  const BudgetHealth({
    required this.householdId,
    required this.periodStartUtc,
    required this.periodEndUtc,
    required this.overallScore,
    required this.scoreBreakdown,
    required this.categoryHealth,
  });

  final String householdId;
  final String periodStartUtc;
  final String periodEndUtc;
  final int overallScore;
  final ScoreBreakdown scoreBreakdown;
  final List<CategoryHealth> categoryHealth;

  factory BudgetHealth.fromJson(Map<String, dynamic> json) {
    return BudgetHealth(
      householdId: json['householdId'] as String,
      periodStartUtc: json['periodStartUtc'] as String,
      periodEndUtc: json['periodEndUtc'] as String,
      overallScore: json['overallScore'] as int,
      scoreBreakdown: ScoreBreakdown.fromJson(
          json['scoreBreakdown'] as Map<String, dynamic>),
      categoryHealth: (json['categoryHealth'] as List<dynamic>)
          .map((e) => CategoryHealth.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
