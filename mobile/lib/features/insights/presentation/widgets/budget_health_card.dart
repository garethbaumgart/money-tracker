import 'package:flutter/material.dart';

import '../../domain/budget_health.dart';

/// Displays the composite budget health score as a colored ring gauge
/// with the score breakdown details.
class BudgetHealthCard extends StatelessWidget {
  const BudgetHealthCard({
    super.key,
    required this.health,
  });

  final BudgetHealth health;

  Color _scoreColor(int score) {
    if (score >= 80) return Colors.green;
    if (score >= 50) return Colors.orange;
    return Colors.red;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scoreColor = _scoreColor(health.overallScore);

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Budget health',
              style: theme.textTheme.titleMedium,
            ),
            const SizedBox(height: 16),
            Center(
              child: SizedBox(
                width: 120,
                height: 120,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    SizedBox(
                      width: 120,
                      height: 120,
                      child: CircularProgressIndicator(
                        value: health.overallScore / 100,
                        strokeWidth: 10,
                        backgroundColor: scoreColor.withValues(alpha: 0.15),
                        valueColor:
                            AlwaysStoppedAnimation<Color>(scoreColor),
                      ),
                    ),
                    Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          '${health.overallScore}',
                          style: theme.textTheme.headlineMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: scoreColor,
                          ),
                        ),
                        Text(
                          'out of 100',
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            _BreakdownRow(
              label: 'Adherence',
              score: health.scoreBreakdown.adherenceScore,
              weight: health.scoreBreakdown.adherenceWeight,
            ),
            const SizedBox(height: 8),
            _BreakdownRow(
              label: 'Velocity',
              score: health.scoreBreakdown.velocityScore,
              weight: health.scoreBreakdown.velocityWeight,
            ),
            const SizedBox(height: 8),
            _BreakdownRow(
              label: 'Bill payment',
              score: health.scoreBreakdown.billPaymentScore,
              weight: health.scoreBreakdown.billPaymentWeight,
            ),
            if (health.categoryHealth.isNotEmpty) ...[
              const SizedBox(height: 16),
              const Divider(),
              const SizedBox(height: 8),
              Text(
                'Category status',
                style: theme.textTheme.titleSmall,
              ),
              const SizedBox(height: 8),
              ...health.categoryHealth.map(
                (cat) => _CategoryStatusRow(category: cat),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _BreakdownRow extends StatelessWidget {
  const _BreakdownRow({
    required this.label,
    required this.score,
    required this.weight,
  });

  final String label;
  final double score;
  final double weight;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final weightPercent = (weight * 100).toStringAsFixed(0);

    return Row(
      children: [
        Expanded(
          child: Text(
            '$label ($weightPercent%)',
            style: theme.textTheme.bodySmall,
          ),
        ),
        SizedBox(
          width: 100,
          child: ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: score / 100,
              minHeight: 6,
              backgroundColor: theme.colorScheme.surfaceContainerHighest,
            ),
          ),
        ),
        const SizedBox(width: 8),
        SizedBox(
          width: 36,
          child: Text(
            '${score.toStringAsFixed(0)}',
            style: theme.textTheme.labelSmall?.copyWith(
              fontWeight: FontWeight.w600,
            ),
            textAlign: TextAlign.end,
          ),
        ),
      ],
    );
  }
}

class _CategoryStatusRow extends StatelessWidget {
  const _CategoryStatusRow({required this.category});

  final CategoryHealth category;

  Color _statusColor(String status) {
    switch (status) {
      case 'OnTrack':
        return Colors.green;
      case 'AtRisk':
        return Colors.orange;
      case 'OverBudget':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  String _statusLabel(String status) {
    switch (status) {
      case 'OnTrack':
        return 'On track';
      case 'AtRisk':
        return 'At risk';
      case 'OverBudget':
        return 'Over budget';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = _statusColor(category.status);

    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(
              color: color,
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              category.categoryName,
              style: theme.textTheme.bodySmall,
            ),
          ),
          Text(
            _statusLabel(category.status),
            style: theme.textTheme.labelSmall?.copyWith(
              color: color,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
