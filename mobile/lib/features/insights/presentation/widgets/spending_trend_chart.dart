import 'package:flutter/material.dart';

import '../../domain/spending_analysis.dart';

/// Displays a bar chart comparing current vs previous period spending
/// for each category.
class SpendingTrendChart extends StatelessWidget {
  const SpendingTrendChart({
    super.key,
    required this.categories,
    required this.period,
  });

  final List<CategorySpending> categories;
  final String period;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (categories.isEmpty) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Center(
            child: Text(
              'No spending data for this period.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
        ),
      );
    }

    final maxSpent = categories.fold<double>(
      0,
      (max, c) {
        final categoryMax =
            c.currentSpent > c.previousSpent ? c.currentSpent : c.previousSpent;
        return categoryMax > max ? categoryMax : max;
      },
    );

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Spending by category ($period)',
              style: theme.textTheme.titleMedium,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                _LegendDot(
                  color: theme.colorScheme.primary,
                  label: 'Current',
                ),
                const SizedBox(width: 16),
                _LegendDot(
                  color: theme.colorScheme.outline,
                  label: 'Previous',
                ),
              ],
            ),
            const SizedBox(height: 16),
            ...categories.map(
              (category) => Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: _CategoryBar(
                  category: category,
                  maxValue: maxSpent,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _LegendDot extends StatelessWidget {
  const _LegendDot({required this.color, required this.label});

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 10,
          height: 10,
          decoration: BoxDecoration(
            color: color,
            shape: BoxShape.circle,
          ),
        ),
        const SizedBox(width: 4),
        Text(label, style: Theme.of(context).textTheme.labelSmall),
      ],
    );
  }
}

class _CategoryBar extends StatelessWidget {
  const _CategoryBar({
    required this.category,
    required this.maxValue,
  });

  final CategorySpending category;
  final double maxValue;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final currentFraction = maxValue > 0 ? category.currentSpent / maxValue : 0.0;
    final previousFraction =
        maxValue > 0 ? category.previousSpent / maxValue : 0.0;

    final changeText = category.changePercent >= 0
        ? '+${category.changePercent.toStringAsFixed(1)}%'
        : '${category.changePercent.toStringAsFixed(1)}%';

    final changeColor = category.changePercent > 50
        ? theme.colorScheme.error
        : category.changePercent > 0
            ? theme.colorScheme.tertiary
            : Colors.green;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Expanded(
              child: Text(
                category.categoryName,
                style: theme.textTheme.bodySmall,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Text(
              changeText,
              style: theme.textTheme.labelSmall?.copyWith(
                color: changeColor,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
        const SizedBox(height: 4),
        ClipRRect(
          borderRadius: BorderRadius.circular(4),
          child: SizedBox(
            height: 8,
            child: Stack(
              children: [
                FractionallySizedBox(
                  widthFactor: previousFraction,
                  child: Container(color: theme.colorScheme.outline.withValues(alpha: 0.4)),
                ),
                FractionallySizedBox(
                  widthFactor: currentFraction,
                  child: Container(color: theme.colorScheme.primary),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 2),
        Text(
          '\$${category.currentSpent.toStringAsFixed(0)} vs \$${category.previousSpent.toStringAsFixed(0)}',
          style: theme.textTheme.labelSmall?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
