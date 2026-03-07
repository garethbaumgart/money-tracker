import 'package:flutter/material.dart';

import '../../domain/spending_analysis.dart';

/// Displays a spending anomaly alert card showing the category name,
/// current and previous amounts, and percentage change.
class AnomalyCard extends StatelessWidget {
  const AnomalyCard({
    super.key,
    required this.anomaly,
  });

  final SpendingAnomaly anomaly;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      color: theme.colorScheme.errorContainer,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Icon(
              Icons.trending_up,
              color: theme.colorScheme.error,
              size: 24,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    anomaly.categoryName,
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: theme.colorScheme.onErrorContainer,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    '\$${anomaly.currentSpent.toStringAsFixed(0)} vs \$${anomaly.previousSpent.toStringAsFixed(0)} last period',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onErrorContainer
                          .withValues(alpha: 0.8),
                    ),
                  ),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 8,
                vertical: 4,
              ),
              decoration: BoxDecoration(
                color: theme.colorScheme.error,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                '+${anomaly.changePercent.toStringAsFixed(0)}%',
                style: theme.textTheme.labelSmall?.copyWith(
                  color: theme.colorScheme.onError,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
