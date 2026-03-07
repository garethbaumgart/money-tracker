import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';

/// Post-purchase confirmation screen.
/// AC-5: Shows purchase success screen after successful purchase.
class PurchaseSuccessScreen extends StatelessWidget {
  const PurchaseSuccessScreen({
    super.key,
    required this.onContinue,
  });

  /// Called when the user taps the continue button.
  final VoidCallback onContinue;

  static const _unlockedFeatures = [
    _FeatureItem(
      icon: Icons.sync,
      label: 'Bank sync',
      description: 'Automatic transaction imports',
    ),
    _FeatureItem(
      icon: Icons.insights,
      label: 'Premium insights',
      description: 'Advanced spending analytics',
    ),
    _FeatureItem(
      icon: Icons.all_inclusive,
      label: 'Unlimited budgets',
      description: 'Create as many budgets as you need',
    ),
    _FeatureItem(
      icon: Icons.notifications_active,
      label: 'Unlimited bill reminders',
      description: 'Never miss a payment',
    ),
    _FeatureItem(
      icon: Icons.download,
      label: 'Data export',
      description: 'Export your financial data anytime',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);
    final scheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: EdgeInsets.all(tokens.space5),
          child: Column(
            children: [
              const Spacer(flex: 1),
              Icon(
                Icons.check_circle_outline,
                size: 80,
                color: tokens.stateSuccess,
              ),
              SizedBox(height: tokens.space4),
              Text(
                'Welcome to Premium!',
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                textAlign: TextAlign.center,
              ),
              SizedBox(height: tokens.space2),
              Text(
                'You now have access to all premium features.',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: tokens.contentSecondary,
                    ),
                textAlign: TextAlign.center,
              ),
              SizedBox(height: tokens.space5),
              ..._unlockedFeatures.map(
                (feature) => Padding(
                  padding: EdgeInsets.only(bottom: tokens.space3),
                  child: _FeatureRow(feature: feature, tokens: tokens),
                ),
              ),
              const Spacer(flex: 2),
              SizedBox(
                width: double.infinity,
                height: 52,
                child: FilledButton(
                  onPressed: onContinue,
                  style: FilledButton.styleFrom(
                    shape: RoundedRectangleBorder(
                      borderRadius: tokens.radiusMedium,
                    ),
                  ),
                  child: Text(
                    'Continue',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: scheme.onPrimary,
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _FeatureItem {
  const _FeatureItem({
    required this.icon,
    required this.label,
    required this.description,
  });

  final IconData icon;
  final String label;
  final String description;
}

class _FeatureRow extends StatelessWidget {
  const _FeatureRow({
    required this.feature,
    required this.tokens,
  });

  final _FeatureItem feature;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(
          feature.icon,
          color: tokens.stateSuccess,
          size: 24,
        ),
        SizedBox(width: tokens.space3),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                feature.label,
                style: Theme.of(context).textTheme.titleSmall,
              ),
              Text(
                feature.description,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: tokens.contentSecondary,
                    ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
