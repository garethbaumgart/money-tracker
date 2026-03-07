import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';
import '../application/feedback_service.dart';
import '../domain/nps_prompt.dart';

/// NPS prompt dialog with a 0-10 score scale.
class NpsPromptDialog extends StatefulWidget {
  const NpsPromptDialog({
    super.key,
    required this.feedbackService,
  });

  /// Shows the NPS prompt dialog.
  /// Returns true if NPS was submitted, false if dismissed.
  static Future<bool?> show({
    required BuildContext context,
    required FeedbackService feedbackService,
  }) {
    return showDialog<bool>(
      context: context,
      builder: (_) => NpsPromptDialog(
        feedbackService: feedbackService,
      ),
    );
  }

  final FeedbackService feedbackService;

  @override
  State<NpsPromptDialog> createState() => _NpsPromptDialogState();
}

class _NpsPromptDialogState extends State<NpsPromptDialog> {
  int? _selectedScore;
  final _commentController = TextEditingController();
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_selectedScore == null) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final prompt = NpsPrompt(
      score: _selectedScore!,
      comment: _commentController.text.isNotEmpty
          ? _commentController.text
          : null,
    );

    final result = await widget.feedbackService.submitNps(prompt);

    if (!mounted) return;

    setState(() {
      _isSubmitting = false;
    });

    switch (result) {
      case FeedbackSuccess():
        Navigator.of(context).pop(true);
      case FeedbackFailure(:final errorMessage):
        setState(() {
          _errorMessage = errorMessage;
        });
    }
  }

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return AlertDialog(
      title: const Text('How likely are you to recommend us?'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Score selector (0-10)
            Wrap(
              spacing: tokens.space1,
              runSpacing: tokens.space1,
              alignment: WrapAlignment.center,
              children: List.generate(11, (index) {
                final isSelected = _selectedScore == index;
                return ChoiceChip(
                  label: Text('$index'),
                  selected: isSelected,
                  onSelected: (_) {
                    setState(() {
                      _selectedScore = index;
                    });
                  },
                );
              }),
            ),
            SizedBox(height: tokens.space2),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Not likely',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: tokens.contentMuted,
                      ),
                ),
                Text(
                  'Very likely',
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: tokens.contentMuted,
                      ),
                ),
              ],
            ),
            SizedBox(height: tokens.space3),

            // Optional comment
            TextField(
              controller: _commentController,
              maxLines: 3,
              maxLength: NpsPrompt.maxCommentLength,
              decoration: const InputDecoration(
                hintText: 'Any additional comments? (optional)',
                border: OutlineInputBorder(),
              ),
            ),

            // Error message
            if (_errorMessage != null)
              Padding(
                padding: EdgeInsets.only(top: tokens.space2),
                child: Text(
                  _errorMessage!,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.error,
                      ),
                ),
              ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: const Text('Not now'),
        ),
        FilledButton(
          onPressed: _selectedScore != null && !_isSubmitting ? _submit : null,
          child: _isSubmitting
              ? const SizedBox(
                  height: 16,
                  width: 16,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text('Submit'),
        ),
      ],
    );
  }
}
