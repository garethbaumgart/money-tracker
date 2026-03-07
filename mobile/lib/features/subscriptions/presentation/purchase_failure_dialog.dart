import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';

/// Error feedback dialog for failed purchases.
/// AC-6: Shows error dialog with retry option.
class PurchaseFailureDialog extends StatelessWidget {
  const PurchaseFailureDialog({
    super.key,
    required this.errorMessage,
    required this.onRetry,
    required this.onDismiss,
  });

  /// Human-readable error message.
  final String errorMessage;

  /// Called when the user taps "Try again".
  final VoidCallback onRetry;

  /// Called when the user dismisses the dialog.
  final VoidCallback onDismiss;

  /// Shows the dialog as a modal.
  static Future<void> show({
    required BuildContext context,
    required String errorMessage,
    required VoidCallback onRetry,
    required VoidCallback onDismiss,
  }) {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => PurchaseFailureDialog(
        errorMessage: errorMessage,
        onRetry: onRetry,
        onDismiss: onDismiss,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return AlertDialog(
      shape: RoundedRectangleBorder(
        borderRadius: tokens.radiusMedium,
      ),
      icon: Icon(
        Icons.error_outline,
        color: tokens.stateDanger,
        size: 40,
      ),
      title: const Text('Purchase failed'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            errorMessage,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: tokens.contentSecondary,
                ),
          ),
          SizedBox(height: tokens.space3),
          Text(
            'If the problem persists, please contact support.',
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: tokens.contentMuted,
                ),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: onDismiss,
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: onRetry,
          child: const Text('Try again'),
        ),
      ],
    );
  }
}
