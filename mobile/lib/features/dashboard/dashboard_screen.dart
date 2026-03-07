import 'package:flutter/material.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';
import 'package:money_tracker/features/dashboard/dashboard_controller.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({super.key, required this.controller});

  final DashboardController controller;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final state = controller.state;

        return RefreshIndicator(
          onRefresh: controller.refresh,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: EdgeInsets.all(tokens.space4),
            children: [
              _DashboardHeader(
                lastUpdatedAt: state.lastUpdatedAt,
                onRefresh: controller.refresh,
              ),
              SizedBox(height: tokens.space3),
              _SummaryCard(summary: state.summary, tokens: tokens),
              SizedBox(height: tokens.space3),
              if (state.isEmpty) ...[
                _EmptyDashboardCard(tokens: tokens),
              ] else ...[
                _BudgetSection(
                  categories: state.categories,
                  tokens: tokens,
                ),
                SizedBox(height: tokens.space3),
                _ActivitySection(
                  transactions: state.recentTransactions,
                  tokens: tokens,
                ),
              ],
              SizedBox(height: tokens.space6),
            ],
          ),
        );
      },
    );
  }
}

class _DashboardHeader extends StatelessWidget {
  const _DashboardHeader({
    required this.lastUpdatedAt,
    required this.onRefresh,
  });

  final DateTime lastUpdatedAt;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);
    final dateLabel = MaterialLocalizations.of(context)
        .formatShortDate(lastUpdatedAt);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Shared dashboard',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              SizedBox(height: tokens.space1),
              Text(
                'Updated $dateLabel',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: tokens.contentSecondary,
                ),
              ),
            ],
          ),
        ),
        OutlinedButton.icon(
          onPressed: () => onRefresh(),
          icon: const Icon(Icons.refresh),
          label: const Text('Refresh'),
        ),
      ],
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.summary, required this.tokens});

  final DashboardSummary summary;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Household summary',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space3),
            Wrap(
              spacing: tokens.space4,
              runSpacing: tokens.space3,
              children: [
                _SummaryMetric(
                  label: 'Allocated',
                  value: summary.totalAllocated,
                ),
                _SummaryMetric(
                  label: 'Spent',
                  value: summary.totalSpent,
                ),
                _SummaryMetric(
                  label: 'Remaining',
                  value: summary.totalRemaining,
                ),
                _SummaryMetric(
                  label: 'Uncategorized',
                  value: summary.uncategorizedSpent,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _SummaryMetric extends StatelessWidget {
  const _SummaryMetric({required this.label, required this.value});

  final String label;
  final double value;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 140,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 6),
          Text(
            _formatCurrency(value),
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ],
      ),
    );
  }
}

class _BudgetSection extends StatelessWidget {
  const _BudgetSection({
    required this.categories,
    required this.tokens,
  });

  final List<DashboardCategorySummary> categories;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    if (categories.isEmpty) {
      return _EmptyBudgetCard(tokens: tokens);
    }

    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Budget progress',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space2),
            ...categories.map(
              (category) => Padding(
                padding: EdgeInsets.only(bottom: tokens.space3),
                child: _CategoryRow(
                  category: category,
                  tokens: tokens,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CategoryRow extends StatelessWidget {
  const _CategoryRow({required this.category, required this.tokens});

  final DashboardCategorySummary category;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    final progressColor = category.remaining < 0
        ? tokens.stateDanger
        : tokens.stateSuccess;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                category.name,
                style: Theme.of(context).textTheme.titleSmall,
              ),
            ),
            Text(
              '${_formatCurrency(category.spent)} / ${_formatCurrency(category.allocated)}',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
          ],
        ),
        SizedBox(height: tokens.space1),
        ClipRRect(
          borderRadius: BorderRadius.circular(999),
          child: LinearProgressIndicator(
            value: category.progress,
            minHeight: 6,
            backgroundColor: tokens.borderSubtle,
            color: progressColor,
          ),
        ),
        SizedBox(height: tokens.space1),
        Text(
          category.remaining < 0
              ? '${_formatCurrency(category.remaining.abs())} over budget'
              : '${_formatCurrency(category.remaining)} remaining',
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: tokens.contentSecondary,
          ),
        ),
      ],
    );
  }
}

class _ActivitySection extends StatelessWidget {
  const _ActivitySection({
    required this.transactions,
    required this.tokens,
  });

  final List<DashboardTransactionSummary> transactions;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    if (transactions.isEmpty) {
      return _EmptyActivityCard(tokens: tokens);
    }

    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Recent activity',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space2),
            ...transactions.map(
              (transaction) => Padding(
                padding: EdgeInsets.only(bottom: tokens.space2),
                child: _TransactionRow(transaction: transaction, tokens: tokens),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TransactionRow extends StatelessWidget {
  const _TransactionRow({required this.transaction, required this.tokens});

  final DashboardTransactionSummary transaction;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    final dateLabel = MaterialLocalizations.of(context)
        .formatMediumDate(transaction.occurredAt);
    final subtitle = [
      transaction.categoryName,
      dateLabel,
    ].whereType<String>().join(' - ');

    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                transaction.description ?? 'Household expense',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              SizedBox(height: tokens.space1),
              Text(
                subtitle,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: tokens.contentSecondary,
                ),
              ),
            ],
          ),
        ),
        Text(
          _formatCurrency(transaction.amount),
          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

class _EmptyDashboardCard extends StatelessWidget {
  const _EmptyDashboardCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'No dashboard data yet',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space1),
            Text(
              'Create a budget category and add a transaction to share progress with your household.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
            SizedBox(height: tokens.space3),
            FilledButton(
              onPressed: null,
              child: const Text('Create a budget'),
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyBudgetCard extends StatelessWidget {
  const _EmptyBudgetCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Budget categories needed',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space1),
            Text(
              'Add categories to track allocations and see progress for each bill.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyActivityCard extends StatelessWidget {
  const _EmptyActivityCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'No recent activity',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space1),
            Text(
              'New transactions will appear here once your household starts spending.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

String _formatCurrency(double value) {
  final formatted = value.abs().toStringAsFixed(2);
  final sign = value < 0 ? '-' : '';
  return '$sign\$$formatted';
}
