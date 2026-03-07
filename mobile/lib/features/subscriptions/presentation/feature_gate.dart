import 'package:flutter/widgets.dart';
import '../application/entitlement_provider.dart';
import '../domain/feature_key.dart';

class FeatureGate extends StatelessWidget {
  const FeatureGate({
    super.key,
    required this.feature,
    required this.entitlementProvider,
    required this.entitled,
    required this.fallback,
  });

  final FeatureKey feature;
  final EntitlementProvider entitlementProvider;
  final Widget entitled;
  final Widget fallback;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: entitlementProvider,
      builder: (context, _) {
        if (entitlementProvider.hasFeature(feature)) {
          return entitled;
        }
        return fallback;
      },
    );
  }
}
