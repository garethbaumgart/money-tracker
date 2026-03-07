import 'package:flutter/material.dart';

import '../application/insights_controller.dart';
import 'widgets/anomaly_card.dart';
import 'widgets/budget_health_card.dart';
import 'widgets/insights_paywall_gate.dart';
import 'widgets/spending_trend_chart.dart';

/// Main insights dashboard screen.
///
/// Displays spending trend charts, budget health score, and anomaly alerts
/// in a stacked card layout. Shows a paywall gate when the user lacks
/// a premium subscription.
class InsightsDashboardScreen extends StatelessWidget {
  const InsightsDashboardScreen({
    super.key,
    required this.controller,
  });

  final InsightsController controller;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final state = controller.state;

        if (state.isLoading) {
          return const Center(
            child: CircularProgressIndicator(),
          );
        }

        if (state.isPremiumRequired) {
          // TODO(#105): Replace SnackBar placeholder with PaywallScreen
          // navigation once dependency injection for OfferingsProvider and
          // PurchaseController is available at this level. See issue AC-1
          // through AC-5 for full acceptance criteria.
          return InsightsPaywallGate(
            onUpgrade: () {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Premium upgrade coming soon'),
                ),
              );
            },
          );
        }

        if (state.hasError) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error_outline, size: 48),
                const SizedBox(height: 8),
                Text(
                  state.errorMessage ?? 'An error occurred.',
                  textAlign: TextAlign.center,
                ),
              ],
            ),
          );
        }

        if (!state.hasData) {
          return const Center(
            child: Text('No insights data available.'),
          );
        }

        return SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Period selector
              _PeriodSelector(
                selectedPeriod: state.selectedPeriod,
                onSelected: controller.selectPeriod,
              ),
              const SizedBox(height: 16),

              // Spending trend chart
              if (state.spendingAnalysis != null)
                SpendingTrendChart(
                  categories: state.spendingAnalysis!.categories,
                  period: state.selectedPeriod,
                ),

              const SizedBox(height: 12),

              // Anomaly alerts
              if (state.spendingAnalysis != null &&
                  state.spendingAnalysis!.anomalies.isNotEmpty) ...[
                Text(
                  'Spending alerts',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 8),
                ...state.spendingAnalysis!.anomalies.map(
                  (anomaly) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: AnomalyCard(anomaly: anomaly),
                  ),
                ),
                const SizedBox(height: 12),
              ],

              // Budget health card
              if (state.budgetHealth != null)
                BudgetHealthCard(health: state.budgetHealth!),
            ],
          ),
        );
      },
    );
  }
}

class _PeriodSelector extends StatelessWidget {
  const _PeriodSelector({
    required this.selectedPeriod,
    required this.onSelected,
  });

  final String selectedPeriod;
  final ValueChanged<String> onSelected;

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<String>(
      showSelectedIcon: false,
      segments: const [
        ButtonSegment<String>(value: '7d', label: Text('7 days')),
        ButtonSegment<String>(value: '30d', label: Text('30 days')),
        ButtonSegment<String>(value: '90d', label: Text('90 days')),
      ],
      selected: {selectedPeriod},
      onSelectionChanged: (selection) {
        onSelected(selection.first);
      },
    );
  }
}
