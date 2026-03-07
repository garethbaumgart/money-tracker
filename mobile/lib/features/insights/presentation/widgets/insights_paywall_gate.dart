import 'package:flutter/material.dart';

/// Displayed when the user does not have a premium subscription.
/// Shows a teaser description of insights features with an upgrade prompt.
class InsightsPaywallGate extends StatelessWidget {
  const InsightsPaywallGate({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.insights,
              size: 64,
              color: theme.colorScheme.primary.withValues(alpha: 0.6),
            ),
            const SizedBox(height: 16),
            Text(
              'Premium Insights',
              style: theme.textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              'Unlock spending trends, budget health scores, and anomaly detection to take control of your finances.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            _FeaturePreview(
              icon: Icons.bar_chart,
              title: 'Spending trends',
              description: 'Compare spending across periods.',
            ),
            const SizedBox(height: 12),
            _FeaturePreview(
              icon: Icons.favorite,
              title: 'Budget health score',
              description: 'See how well you stick to your budget.',
            ),
            const SizedBox(height: 12),
            _FeaturePreview(
              icon: Icons.warning_amber,
              title: 'Anomaly alerts',
              description: 'Get notified of unusual spending spikes.',
            ),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: () {
                // TODO: Navigate to subscription/paywall screen.
              },
              icon: const Icon(Icons.star),
              label: const Text('Upgrade to Premium'),
            ),
          ],
        ),
      ),
    );
  }
}

class _FeaturePreview extends StatelessWidget {
  const _FeaturePreview({
    required this.icon,
    required this.title,
    required this.description,
  });

  final IconData icon;
  final String title;
  final String description;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 20, color: theme.colorScheme.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: theme.textTheme.titleSmall),
              Text(
                description,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
