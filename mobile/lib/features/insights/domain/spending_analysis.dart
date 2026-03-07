class CategorySpending {
  const CategorySpending({
    required this.categoryId,
    required this.categoryName,
    required this.currentSpent,
    required this.previousSpent,
    required this.changePercent,
  });

  final String categoryId;
  final String categoryName;
  final double currentSpent;
  final double previousSpent;
  final double changePercent;

  factory CategorySpending.fromJson(Map<String, dynamic> json) {
    return CategorySpending(
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      currentSpent: (json['currentSpent'] as num).toDouble(),
      previousSpent: (json['previousSpent'] as num).toDouble(),
      changePercent: (json['changePercent'] as num).toDouble(),
    );
  }
}

class SpendingAnomaly {
  const SpendingAnomaly({
    required this.categoryId,
    required this.categoryName,
    required this.currentSpent,
    required this.previousSpent,
    required this.changePercent,
  });

  final String categoryId;
  final String categoryName;
  final double currentSpent;
  final double previousSpent;
  final double changePercent;

  factory SpendingAnomaly.fromJson(Map<String, dynamic> json) {
    return SpendingAnomaly(
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      currentSpent: (json['currentSpent'] as num).toDouble(),
      previousSpent: (json['previousSpent'] as num).toDouble(),
      changePercent: (json['changePercent'] as num).toDouble(),
    );
  }
}

class TopCategory {
  const TopCategory({
    required this.categoryId,
    required this.categoryName,
    required this.amount,
    required this.percentOfTotal,
  });

  final String categoryId;
  final String categoryName;
  final double amount;
  final double percentOfTotal;

  factory TopCategory.fromJson(Map<String, dynamic> json) {
    return TopCategory(
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      amount: (json['amount'] as num).toDouble(),
      percentOfTotal: (json['percentOfTotal'] as num).toDouble(),
    );
  }
}

class SpendingAnalysis {
  const SpendingAnalysis({
    required this.householdId,
    required this.period,
    required this.periodStartUtc,
    required this.periodEndUtc,
    required this.totalSpent,
    required this.previousPeriodTotalSpent,
    required this.spendingChangePercent,
    required this.categories,
    required this.anomalies,
    required this.topCategories,
  });

  final String householdId;
  final String period;
  final String periodStartUtc;
  final String periodEndUtc;
  final double totalSpent;
  final double previousPeriodTotalSpent;
  final double spendingChangePercent;
  final List<CategorySpending> categories;
  final List<SpendingAnomaly> anomalies;
  final List<TopCategory> topCategories;

  factory SpendingAnalysis.fromJson(Map<String, dynamic> json) {
    return SpendingAnalysis(
      householdId: json['householdId'] as String,
      period: json['period'] as String,
      periodStartUtc: json['periodStartUtc'] as String,
      periodEndUtc: json['periodEndUtc'] as String,
      totalSpent: (json['totalSpent'] as num).toDouble(),
      previousPeriodTotalSpent:
          (json['previousPeriodTotalSpent'] as num).toDouble(),
      spendingChangePercent:
          (json['spendingChangePercent'] as num).toDouble(),
      categories: (json['categories'] as List<dynamic>)
          .map((e) => CategorySpending.fromJson(e as Map<String, dynamic>))
          .toList(),
      anomalies: (json['anomalies'] as List<dynamic>)
          .map((e) => SpendingAnomaly.fromJson(e as Map<String, dynamic>))
          .toList(),
      topCategories: (json['topCategories'] as List<dynamic>)
          .map((e) => TopCategory.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
