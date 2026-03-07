import 'package:flutter/foundation.dart';

import '../domain/spending_analysis.dart';
import '../domain/budget_health.dart';
import '../infrastructure/insights_api_client.dart';

enum InsightsLoadState { idle, loading, loaded, error, premiumRequired }

class InsightsState {
  InsightsState({
    required this.loadState,
    this.spendingAnalysis,
    this.budgetHealth,
    this.selectedPeriod = '30d',
    this.errorMessage,
  });

  final InsightsLoadState loadState;
  final SpendingAnalysis? spendingAnalysis;
  final BudgetHealth? budgetHealth;
  final String selectedPeriod;
  final String? errorMessage;

  bool get isLoading => loadState == InsightsLoadState.loading;
  bool get isPremiumRequired =>
      loadState == InsightsLoadState.premiumRequired;
  bool get hasData => loadState == InsightsLoadState.loaded;
  bool get hasError => loadState == InsightsLoadState.error;

  InsightsState copyWith({
    InsightsLoadState? loadState,
    SpendingAnalysis? spendingAnalysis,
    BudgetHealth? budgetHealth,
    String? selectedPeriod,
    String? errorMessage,
  }) {
    return InsightsState(
      loadState: loadState ?? this.loadState,
      spendingAnalysis: spendingAnalysis ?? this.spendingAnalysis,
      budgetHealth: budgetHealth ?? this.budgetHealth,
      selectedPeriod: selectedPeriod ?? this.selectedPeriod,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }

  factory InsightsState.initial() {
    return InsightsState(loadState: InsightsLoadState.idle);
  }

  factory InsightsState.sample() {
    return InsightsState(
      loadState: InsightsLoadState.loaded,
      selectedPeriod: '30d',
      spendingAnalysis: SpendingAnalysis(
        householdId: 'sample-household',
        period: '30d',
        periodStartUtc: DateTime.now()
            .subtract(const Duration(days: 30))
            .toIso8601String(),
        periodEndUtc: DateTime.now().toIso8601String(),
        totalSpent: 2450.00,
        previousPeriodTotalSpent: 2100.00,
        spendingChangePercent: 16.67,
        categories: const [
          CategorySpending(
            categoryId: 'cat-1',
            categoryName: 'Groceries',
            currentSpent: 820.00,
            previousSpent: 700.00,
            changePercent: 17.14,
          ),
          CategorySpending(
            categoryId: 'cat-2',
            categoryName: 'Dining',
            currentSpent: 450.00,
            previousSpent: 280.00,
            changePercent: 60.71,
          ),
          CategorySpending(
            categoryId: 'cat-3',
            categoryName: 'Transport',
            currentSpent: 380.00,
            previousSpent: 420.00,
            changePercent: -9.52,
          ),
          CategorySpending(
            categoryId: 'cat-4',
            categoryName: 'Utilities',
            currentSpent: 310.00,
            previousSpent: 300.00,
            changePercent: 3.33,
          ),
        ],
        anomalies: const [
          SpendingAnomaly(
            categoryId: 'cat-2',
            categoryName: 'Dining',
            currentSpent: 450.00,
            previousSpent: 280.00,
            changePercent: 60.71,
          ),
        ],
        topCategories: const [
          TopCategory(
            categoryId: 'cat-1',
            categoryName: 'Groceries',
            amount: 820.00,
            percentOfTotal: 33.47,
          ),
          TopCategory(
            categoryId: 'cat-2',
            categoryName: 'Dining',
            amount: 450.00,
            percentOfTotal: 18.37,
          ),
          TopCategory(
            categoryId: 'cat-3',
            categoryName: 'Transport',
            amount: 380.00,
            percentOfTotal: 15.51,
          ),
        ],
      ),
      budgetHealth: BudgetHealth(
        householdId: 'sample-household',
        periodStartUtc: DateTime.now()
            .subtract(const Duration(days: 15))
            .toIso8601String(),
        periodEndUtc: DateTime.now()
            .add(const Duration(days: 15))
            .toIso8601String(),
        overallScore: 72,
        scoreBreakdown: const ScoreBreakdown(
          adherenceScore: 75.0,
          adherenceWeight: 0.40,
          velocityScore: 65.0,
          velocityWeight: 0.35,
          billPaymentScore: 80.0,
          billPaymentWeight: 0.25,
        ),
        categoryHealth: const [
          CategoryHealth(
            categoryId: 'cat-1',
            categoryName: 'Groceries',
            allocated: 900.0,
            spent: 820.0,
            status: 'AtRisk',
          ),
          CategoryHealth(
            categoryId: 'cat-2',
            categoryName: 'Dining',
            allocated: 400.0,
            spent: 450.0,
            status: 'OverBudget',
          ),
          CategoryHealth(
            categoryId: 'cat-3',
            categoryName: 'Transport',
            allocated: 500.0,
            spent: 380.0,
            status: 'OnTrack',
          ),
          CategoryHealth(
            categoryId: 'cat-4',
            categoryName: 'Utilities',
            allocated: 350.0,
            spent: 310.0,
            status: 'AtRisk',
          ),
        ],
      ),
    );
  }
}

class InsightsController extends ChangeNotifier {
  InsightsController({
    InsightsApiClient? apiClient,
    InsightsState? initialState,
  })  : _apiClient = apiClient ?? InsightsApiClient(),
        _state = initialState ?? InsightsState.initial();

  final InsightsApiClient _apiClient;
  InsightsState _state;
  bool _seeded = false;

  InsightsState get state => _state;

  void selectPeriod(String period) {
    if (_state.selectedPeriod == period) return;
    _state = _state.copyWith(selectedPeriod: period);
    notifyListeners();
  }

  Future<void> refresh({
    required String householdId,
    required String bearerToken,
  }) async {
    _state = _state.copyWith(loadState: InsightsLoadState.loading);
    notifyListeners();

    try {
      final spending = await _apiClient.getSpendingSummary(
        householdId: householdId,
        period: _state.selectedPeriod,
        bearerToken: bearerToken,
      );

      final health = await _apiClient.getBudgetHealth(
        householdId: householdId,
        bearerToken: bearerToken,
      );

      if (spending == null && health == null) {
        _state = _state.copyWith(
          loadState: InsightsLoadState.premiumRequired,
        );
      } else {
        _state = _state.copyWith(
          loadState: InsightsLoadState.loaded,
          spendingAnalysis: spending,
          budgetHealth: health,
        );
      }
    } catch (e) {
      _state = _state.copyWith(
        loadState: InsightsLoadState.error,
        errorMessage: e.toString(),
      );
    }

    notifyListeners();
  }

  void seedSample() {
    if (_seeded) return;
    _seeded = true;
    _state = InsightsState.sample();
    notifyListeners();
  }
}
