import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';

/// Purchase CTA button with loading state.
/// AC-4: Shows loading spinner during purchase.
class PurchaseButton extends StatelessWidget {
  const PurchaseButton({
    super.key,
    required this.onPressed,
    required this.isLoading,
    this.label = 'Subscribe',
    this.isDisabled = false,
  });

  /// Called when the button is tapped.
  final VoidCallback? onPressed;

  /// Whether the purchase is currently in progress.
  final bool isLoading;

  /// Button label text.
  final String label;

  /// Whether the button is disabled (e.g. no plan selected).
  final bool isDisabled;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return SizedBox(
      width: double.infinity,
      height: 52,
      child: FilledButton(
        onPressed: isLoading || isDisabled ? null : onPressed,
        style: FilledButton.styleFrom(
          shape: RoundedRectangleBorder(
            borderRadius: tokens.radiusMedium,
          ),
        ),
        child: isLoading
            ? SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  color: Theme.of(context).colorScheme.onPrimary,
                ),
              )
            : Text(
                label,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: Theme.of(context).colorScheme.onPrimary,
                      fontWeight: FontWeight.w600,
                    ),
              ),
      ),
    );
  }
}
