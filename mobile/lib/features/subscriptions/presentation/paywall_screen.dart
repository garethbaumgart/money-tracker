import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';
import '../domain/offering.dart';
import '../domain/purchase_result.dart';
import 'paywall_controller.dart';
import 'plan_card.dart';
import 'purchase_button.dart';
import 'purchase_failure_dialog.dart';
import 'purchase_success_screen.dart';

/// Main paywall screen with annual-first pricing presentation.
/// AC-1: Annual plan displayed as primary (highlighted, recommended).
/// AC-2: Pricing loaded from RevenueCat Offerings.
/// AC-10: Accessible from settings, feature gates, and direct navigation.
/// AC-13: Supports variant parameter for A/B test readiness.
/// AC-15: Follows Flutter UX theming standards.
class PaywallScreen extends StatefulWidget {
  const PaywallScreen({
    super.key,
    required this.controller,
    this.source,
    this.variant = 'A',
    this.isTrialEligible = false,
    this.trialDays = 0,
  });

  /// Route name for named navigation.
  static const routeName = '/paywall';

  /// Navigates to the paywall screen.
  /// Returns true if a purchase was completed, false otherwise.
  static Future<bool?> navigate(
    BuildContext context, {
    required PaywallController controller,
    String? source,
    String variant = 'A',
    bool isTrialEligible = false,
    int trialDays = 0,
  }) {
    return Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => PaywallScreen(
          controller: controller,
          source: source,
          variant: variant,
          isTrialEligible: isTrialEligible,
          trialDays: trialDays,
        ),
      ),
    );
  }

  /// Paywall state controller.
  final PaywallController controller;

  /// Source that triggered the paywall (for analytics).
  final String? source;

  /// A/B test variant. AC-13.
  final String variant;

  /// Whether the user is eligible for a free trial. AC-11.
  final bool isTrialEligible;

  /// Number of trial days, if eligible.
  final int trialDays;

  @override
  State<PaywallScreen> createState() => _PaywallScreenState();
}

class _PaywallScreenState extends State<PaywallScreen> {
  @override
  void initState() {
    super.initState();
    widget.controller.loadOfferings();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.controller,
      builder: (context, _) {
        final state = widget.controller.state;

        if (state == PaywallState.success) {
          return PurchaseSuccessScreen(
            onContinue: () => Navigator.of(context).pop(true),
          );
        }

        return Scaffold(
          appBar: AppBar(
            title: const Text('Upgrade to Premium'),
            leading: IconButton(
              icon: const Icon(Icons.close),
              onPressed: () => Navigator.of(context).pop(false),
            ),
          ),
          body: _buildBody(context, state),
        );
      },
    );
  }

  Widget _buildBody(BuildContext context, PaywallState state) {
    final tokens = AppThemeTokens.of(context);

    switch (state) {
      case PaywallState.loading:
        return const Center(child: CircularProgressIndicator());

      case PaywallState.empty:
        return _EmptyState(
          tokens: tokens,
          errorMessage: widget.controller.errorMessage ??
              'No plans are currently available.',
          onRetry: () => widget.controller.loadOfferings(),
        );

      case PaywallState.error:
        return _EmptyState(
          tokens: tokens,
          errorMessage: widget.controller.errorMessage ??
              'Something went wrong. Please try again.',
          onRetry: () {
            widget.controller.resetError();
            widget.controller.loadOfferings();
          },
        );

      case PaywallState.loaded:
      case PaywallState.purchasing:
      case PaywallState.success:
        return _LoadedPaywall(
          controller: widget.controller,
          isTrialEligible: widget.isTrialEligible,
          trialDays: widget.trialDays,
          onPurchase: _handlePurchase,
        );
    }
  }

  Future<void> _handlePurchase() async {
    final result = await widget.controller.purchaseSelectedPackage();
    if (result == null || !mounted) return;

    switch (result) {
      case PurchaseFailed(:final errorMessage):
        if (mounted) {
          PurchaseFailureDialog.show(
            context: context,
            errorMessage: errorMessage,
            onRetry: () {
              Navigator.of(context).pop();
              widget.controller.resetError();
            },
            onDismiss: () {
              Navigator.of(context).pop();
              widget.controller.resetError();
            },
          );
        }
      case PurchasePending(:final message):
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(message)),
          );
        }
      case PurchaseSuccess():
      case PurchaseCancelled():
        // Handled by state transitions in the controller.
        break;
    }
  }
}

class _LoadedPaywall extends StatelessWidget {
  const _LoadedPaywall({
    required this.controller,
    required this.isTrialEligible,
    required this.trialDays,
    required this.onPurchase,
  });

  final PaywallController controller;
  final bool isTrialEligible;
  final int trialDays;
  final VoidCallback onPurchase;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);
    final scheme = Theme.of(context).colorScheme;

    return SingleChildScrollView(
      padding: EdgeInsets.all(tokens.space4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // AC-11: Trial information when eligible.
          if (isTrialEligible && trialDays > 0)
            _TrialBanner(trialDays: trialDays, tokens: tokens, scheme: scheme),

          // Header
          Text(
            'Unlock all features',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
          ),
          SizedBox(height: tokens.space2),
          Text(
            'Get the most out of Money Tracker with Premium.',
            style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                  color: tokens.contentSecondary,
                ),
          ),
          SizedBox(height: tokens.space5),

          // Feature comparison
          _FeatureList(tokens: tokens),
          SizedBox(height: tokens.space5),

          // Plan cards — annual first (AC-1)
          Text(
            'Choose your plan',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
          ),
          SizedBox(height: tokens.space3),

          if (controller.annualPackage != null)
            Padding(
              padding: EdgeInsets.only(bottom: tokens.space3),
              child: PlanCard(
                package: controller.annualPackage!,
                isSelected:
                    controller.selectedPackage == controller.annualPackage,
                onTap: () =>
                    controller.selectPackage(controller.annualPackage!),
                monthlyPackage: controller.monthlyPackage,
                isRecommended: true,
              ),
            ),

          if (controller.monthlyPackage != null)
            Padding(
              padding: EdgeInsets.only(bottom: tokens.space3),
              child: PlanCard(
                package: controller.monthlyPackage!,
                isSelected:
                    controller.selectedPackage == controller.monthlyPackage,
                onTap: () =>
                    controller.selectPackage(controller.monthlyPackage!),
              ),
            ),

          SizedBox(height: tokens.space3),

          // Purchase button
          PurchaseButton(
            onPressed: onPurchase,
            isLoading: controller.isPurchasing,
            label: isTrialEligible
                ? 'Start free trial'
                : 'Subscribe',
            isDisabled: controller.selectedPackage == null,
          ),

          SizedBox(height: tokens.space4),

          // Terms and privacy
          _LegalLinks(tokens: tokens),
        ],
      ),
    );
  }
}

class _TrialBanner extends StatelessWidget {
  const _TrialBanner({
    required this.trialDays,
    required this.tokens,
    required this.scheme,
  });

  final int trialDays;
  final AppThemeTokens tokens;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: tokens.space4),
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Color.lerp(scheme.surface, scheme.primary, 0.08),
          borderRadius: tokens.radiusMedium,
          border: Border.all(
            color: Color.lerp(scheme.surface, scheme.primary, 0.25)!,
          ),
        ),
        child: Padding(
          padding: EdgeInsets.all(tokens.space4),
          child: Row(
            children: [
              Icon(Icons.card_giftcard, color: scheme.primary, size: 28),
              SizedBox(width: tokens.space3),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Start your $trialDays-day free trial',
                      style:
                          Theme.of(context).textTheme.titleSmall?.copyWith(
                                fontWeight: FontWeight.w600,
                              ),
                    ),
                    SizedBox(height: tokens.space1),
                    Text(
                      'Try all premium features free. Cancel anytime.',
                      style:
                          Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: tokens.contentSecondary,
                              ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _FeatureList extends StatelessWidget {
  const _FeatureList({required this.tokens});

  final AppThemeTokens tokens;

  static const _features = [
    _Feature('Bank sync', true),
    _Feature('Premium insights', true),
    _Feature('Unlimited budgets', true),
    _Feature('Unlimited bill reminders', true),
    _Feature('Data export', true),
  ];

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        borderRadius: tokens.radiusMedium,
        border: Border.all(color: tokens.borderSubtle),
        color: tokens.surfaceMuted,
      ),
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Premium includes',
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
            ),
            SizedBox(height: tokens.space3),
            ..._features.map(
              (feature) => Padding(
                padding: EdgeInsets.only(bottom: tokens.space2),
                child: Row(
                  children: [
                    Icon(
                      Icons.check_circle,
                      size: 20,
                      color: tokens.stateSuccess,
                    ),
                    SizedBox(width: tokens.space2),
                    Text(
                      feature.label,
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Feature {
  const _Feature(this.label, this.isPremium);

  final String label;
  final bool isPremium;
}

class _LegalLinks extends StatelessWidget {
  const _LegalLinks({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          'Recurring billing. Cancel anytime.',
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentMuted,
              ),
        ),
        SizedBox(height: tokens.space2),
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            TextButton(
              onPressed: () {
                // Will be wired to actual terms URL in production.
              },
              child: Text(
                'Terms of Service',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
            Text(
              ' and ',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: tokens.contentMuted,
                  ),
            ),
            TextButton(
              onPressed: () {
                // Will be wired to actual privacy URL in production.
              },
              child: Text(
                'Privacy Policy',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({
    required this.tokens,
    required this.errorMessage,
    required this.onRetry,
  });

  final AppThemeTokens tokens;
  final String errorMessage;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.all(tokens.space5),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.cloud_off,
              size: 48,
              color: tokens.contentMuted,
            ),
            SizedBox(height: tokens.space4),
            Text(
              errorMessage,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                    color: tokens.contentSecondary,
                  ),
            ),
            SizedBox(height: tokens.space4),
            OutlinedButton(
              onPressed: onRetry,
              child: const Text('Try again'),
            ),
          ],
        ),
      ),
    );
  }
}
