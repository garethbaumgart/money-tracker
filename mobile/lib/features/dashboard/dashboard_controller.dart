import 'package:flutter/foundation.dart';

class DashboardSummary {
  const DashboardSummary({
    required this.totalAllocated,
    required this.totalSpent,
    required this.totalRemaining,
    required this.uncategorizedSpent,
  });

  final double totalAllocated;
  final double totalSpent;
  final double totalRemaining;
  final double uncategorizedSpent;

  DashboardSummary copyWith({
    double? totalAllocated,
    double? totalSpent,
    double? totalRemaining,
    double? uncategorizedSpent,
  }) {
    return DashboardSummary(
      totalAllocated: totalAllocated ?? this.totalAllocated,
      totalSpent: totalSpent ?? this.totalSpent,
      totalRemaining: totalRemaining ?? this.totalRemaining,
      uncategorizedSpent: uncategorizedSpent ?? this.uncategorizedSpent,
    );
  }

  factory DashboardSummary.empty() {
    return const DashboardSummary(
      totalAllocated: 0,
      totalSpent: 0,
      totalRemaining: 0,
      uncategorizedSpent: 0,
    );
  }
}

class DashboardCategorySummary {
  const DashboardCategorySummary({
    required this.id,
    required this.name,
    required this.allocated,
    required this.spent,
  });

  final String id;
  final String name;
  final double allocated;
  final double spent;

  double get remaining => allocated - spent;

  double get progress {
    if (allocated <= 0) {
      return 0;
    }

    return (spent / allocated).clamp(0, 1);
  }
}

class DashboardTransactionSummary {
  const DashboardTransactionSummary({
    required this.id,
    required this.amount,
    required this.occurredAt,
    this.description,
    this.categoryName,
  });

  final String id;
  final double amount;
  final DateTime occurredAt;
  final String? description;
  final String? categoryName;
}

class DashboardState {
  const DashboardState({
    required this.summary,
    required this.categories,
    required this.recentTransactions,
    required this.lastUpdatedAt,
    required this.refreshCount,
    required this.isLoading,
  });

  final DashboardSummary summary;
  final List<DashboardCategorySummary> categories;
  final List<DashboardTransactionSummary> recentTransactions;
  final DateTime lastUpdatedAt;
  final int refreshCount;
  final bool isLoading;

  bool get hasBudgetData =>
      categories.isNotEmpty || summary.totalAllocated > 0;

  bool get hasActivity => recentTransactions.isNotEmpty;

  bool get isEmpty => !hasBudgetData && !hasActivity;

  bool get isPartial => hasBudgetData && !hasActivity;

  DashboardState copyWith({
    DashboardSummary? summary,
    List<DashboardCategorySummary>? categories,
    List<DashboardTransactionSummary>? recentTransactions,
    DateTime? lastUpdatedAt,
    int? refreshCount,
    bool? isLoading,
  }) {
    return DashboardState(
      summary: summary ?? this.summary,
      categories: categories ?? this.categories,
      recentTransactions: recentTransactions ?? this.recentTransactions,
      lastUpdatedAt: lastUpdatedAt ?? this.lastUpdatedAt,
      refreshCount: refreshCount ?? this.refreshCount,
      isLoading: isLoading ?? this.isLoading,
    );
  }

  factory DashboardState.empty() {
    return DashboardState(
      summary: DashboardSummary.empty(),
      categories: const [],
      recentTransactions: const [],
      lastUpdatedAt: DateTime.now(),
      refreshCount: 0,
      isLoading: false,
    );
  }

  factory DashboardState.sample() {
    final categories = <DashboardCategorySummary>[
      const DashboardCategorySummary(
        id: 'cat-1',
        name: 'Groceries',
        allocated: 520,
        spent: 312,
      ),
      const DashboardCategorySummary(
        id: 'cat-2',
        name: 'Utilities',
        allocated: 210,
        spent: 178,
      ),
      const DashboardCategorySummary(
        id: 'cat-3',
        name: 'Dining',
        allocated: 160,
        spent: 95,
      ),
    ];
    final transactions = <DashboardTransactionSummary>[
      DashboardTransactionSummary(
        id: 'tx-1',
        amount: 92.30,
        occurredAt: DateTime.now().subtract(const Duration(days: 1)),
        description: 'Grocer run',
        categoryName: 'Groceries',
      ),
      DashboardTransactionSummary(
        id: 'tx-2',
        amount: 64.10,
        occurredAt: DateTime.now().subtract(const Duration(days: 2)),
        description: 'Electric bill',
        categoryName: 'Utilities',
      ),
      DashboardTransactionSummary(
        id: 'tx-3',
        amount: 28.50,
        occurredAt: DateTime.now().subtract(const Duration(days: 3)),
        description: 'Lunch delivery',
        categoryName: 'Dining',
      ),
    ];

    final totalAllocated = categories.fold<double>(
      0,
      (sum, category) => sum + category.allocated,
    );
    final totalSpent = categories.fold<double>(
      0,
      (sum, category) => sum + category.spent,
    );
    final uncategorizedSpent = 42.0;

    return DashboardState(
      summary: DashboardSummary(
        totalAllocated: totalAllocated,
        totalSpent: totalSpent + uncategorizedSpent,
        totalRemaining: totalAllocated - totalSpent - uncategorizedSpent,
        uncategorizedSpent: uncategorizedSpent,
      ),
      categories: categories,
      recentTransactions: transactions,
      lastUpdatedAt: DateTime.now(),
      refreshCount: 1,
      isLoading: false,
    );
  }
}

class DashboardController extends ChangeNotifier {
  DashboardController({DashboardState? initialState})
      : _state = initialState ?? DashboardState.empty();

  DashboardState _state;
  bool _seeded = false;

  DashboardState get state => _state;

  Future<void> refresh() async {
    _state = _state.copyWith(
      lastUpdatedAt: DateTime.now(),
      refreshCount: _state.refreshCount + 1,
      isLoading: false,
    );
    notifyListeners();
  }

  void seedSample() {
    if (_seeded) {
      return;
    }

    _seeded = true;
    _state = DashboardState.sample();
    notifyListeners();
  }
}
