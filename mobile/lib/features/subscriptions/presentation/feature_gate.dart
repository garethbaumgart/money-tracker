import 'package:flutter/widgets.dart';
import '../application/entitlement_provider.dart';
import '../domain/feature_key.dart';

/// Callback type for navigating to the paywall with a source parameter.
typedef OnUpgradeTap = void Function(String source);

/// Gates premium features and optionally navigates to the paywall.
/// AC-9: FeatureGate upgrade CTA navigates to paywall with source parameter.
class FeatureGate extends StatelessWidget {
  const FeatureGate({
    super.key,
    required this.feature,
    required this.entitlementProvider,
    required this.entitled,
    required this.fallback,
    this.onUpgradeTap,
  });

  final FeatureKey feature;
  final EntitlementProvider entitlementProvider;
  final Widget entitled;
  final Widget fallback;

  /// Called when the user taps the upgrade CTA.
  /// Provides a source string identifying this feature gate (e.g. "bankSync").
  /// If null, the fallback widget is rendered as-is without tap handling.
  final OnUpgradeTap? onUpgradeTap;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: entitlementProvider,
      builder: (context, _) {
        if (entitlementProvider.hasFeature(feature)) {
          return entitled;
        }
        if (onUpgradeTap != null) {
          return GestureDetector(
            onTap: () => onUpgradeTap!(feature.name),
            child: fallback,
          );
        }
        return fallback;
      },
    );
  }
}
