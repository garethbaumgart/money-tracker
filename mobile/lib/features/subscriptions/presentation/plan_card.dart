import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';
import '../domain/offering.dart';

/// Displays a subscription plan option (annual or monthly).
/// AC-1: Annual plan shown as primary with highlighted styling.
/// AC-3: Annual plan shows per-month equivalent and savings percentage.
class PlanCard extends StatelessWidget {
  const PlanCard({
    super.key,
    required this.package,
    required this.isSelected,
    required this.onTap,
    this.monthlyPackage,
    this.isRecommended = false,
  });

  /// The package this card represents.
  final Package package;

  /// Whether this card is currently selected.
  final bool isSelected;

  /// Called when the user taps this card.
  final VoidCallback onTap;

  /// The monthly package, used to calculate savings for annual plans.
  final Package? monthlyPackage;

  /// Whether to show the "Recommended" badge.
  final bool isRecommended;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);
    final scheme = Theme.of(context).colorScheme;

    final savingsPercent = monthlyPackage != null
        ? package.savingsPercentVsMonthly(
            monthlyPackage!.priceAmountMicros,
          )
        : 0;

    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        curve: Curves.easeInOut,
        decoration: BoxDecoration(
          borderRadius: tokens.radiusMedium,
          border: Border.all(
            color: isSelected ? scheme.primary : tokens.borderSubtle,
            width: isSelected ? 2 : 1,
          ),
          color: isSelected
              ? Color.lerp(scheme.primary, tokens.surfaceElevated, 0.92)
              : tokens.surfaceElevated,
        ),
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    _planTitle,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                ),
                if (isRecommended && savingsPercent > 0)
                  _SavingsBadge(
                    savingsPercent: savingsPercent,
                    tokens: tokens,
                    scheme: scheme,
                  ),
                SizedBox(width: tokens.space2),
                _SelectionIndicator(
                  isSelected: isSelected,
                  scheme: scheme,
                ),
              ],
            ),
            SizedBox(height: tokens.space2),
            Text(
              package.priceString,
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
            ),
            SizedBox(height: tokens.space1),
            Text(
              _periodLabel,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: tokens.contentSecondary,
                  ),
            ),
            if (package.packageType == PackageType.annual) ...[
              SizedBox(height: tokens.space1),
              Text(
                _perMonthLabel,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: tokens.contentMuted,
                    ),
              ),
            ],
            if (package.introPrice != null && package.introPrice!.isFreeTrial) ...[
              SizedBox(height: tokens.space2),
              _TrialBadge(
                introPrice: package.introPrice!,
                tokens: tokens,
                scheme: scheme,
              ),
            ],
          ],
        ),
      ),
    );
  }

  String get _planTitle {
    switch (package.packageType) {
      case PackageType.annual:
        return 'Annual';
      case PackageType.monthly:
        return 'Monthly';
      case PackageType.weekly:
        return 'Weekly';
      case PackageType.lifetime:
        return 'Lifetime';
      case PackageType.custom:
        return package.identifier;
    }
  }

  String get _periodLabel {
    return 'per ${package.period.displayLabel}';
  }

  String get _perMonthLabel {
    if (package.packageType != PackageType.annual) return '';
    final monthlyMicros = package.monthlyEquivalentMicros;
    // Format to a readable string — approximate since we use micros.
    final monthlyDollars = monthlyMicros / 1000000;
    return 'That\'s \$${monthlyDollars.toStringAsFixed(2)}/month';
  }
}

class _SavingsBadge extends StatelessWidget {
  const _SavingsBadge({
    required this.savingsPercent,
    required this.tokens,
    required this.scheme,
  });

  final int savingsPercent;
  final AppThemeTokens tokens;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Color.lerp(scheme.surface, tokens.stateSuccess, 0.15),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        child: Text(
          'Save $savingsPercent%',
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: tokens.stateSuccess,
                fontWeight: FontWeight.w700,
              ),
        ),
      ),
    );
  }
}

class _SelectionIndicator extends StatelessWidget {
  const _SelectionIndicator({
    required this.isSelected,
    required this.scheme,
  });

  final bool isSelected;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return Icon(
      isSelected ? Icons.radio_button_checked : Icons.radio_button_off,
      color: isSelected ? scheme.primary : scheme.outline,
      size: 22,
    );
  }
}

class _TrialBadge extends StatelessWidget {
  const _TrialBadge({
    required this.introPrice,
    required this.tokens,
    required this.scheme,
  });

  final IntroPrice introPrice;
  final AppThemeTokens tokens;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Color.lerp(scheme.surface, scheme.primary, 0.1),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        child: Text(
          '${introPrice.totalTrialDays}-day free trial',
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: scheme.primary,
                fontWeight: FontWeight.w600,
              ),
        ),
      ),
    );
  }
}
