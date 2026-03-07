import 'package:flutter/material.dart';
import '../../../app/theme/app_theme_tokens.dart';
import '../application/feedback_service.dart';
import '../domain/feedback_category.dart';
import '../domain/feedback_submission.dart';

/// Feedback form screen accessible from settings.
/// AC-11: Accessible from settings page.
class FeedbackFormScreen extends StatefulWidget {
  const FeedbackFormScreen({
    super.key,
    required this.feedbackService,
    this.screenName,
    this.appVersion,
    this.deviceModel,
    this.osVersion,
  });

  /// Route name for navigation.
  static const routeName = '/feedback';

  /// Navigates to the feedback form screen.
  static Future<bool?> navigate(
    BuildContext context, {
    required FeedbackService feedbackService,
    String? screenName,
    String? appVersion,
    String? deviceModel,
    String? osVersion,
  }) {
    return Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => FeedbackFormScreen(
          feedbackService: feedbackService,
          screenName: screenName,
          appVersion: appVersion,
          deviceModel: deviceModel,
          osVersion: osVersion,
        ),
      ),
    );
  }

  final FeedbackService feedbackService;
  final String? screenName;
  final String? appVersion;
  final String? deviceModel;
  final String? osVersion;

  @override
  State<FeedbackFormScreen> createState() => _FeedbackFormScreenState();
}

class _FeedbackFormScreenState extends State<FeedbackFormScreen> {
  FeedbackCategory _selectedCategory = FeedbackCategory.general;
  final _descriptionController = TextEditingController();
  int _rating = 3;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void dispose() {
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final submission = FeedbackSubmission(
      category: _selectedCategory,
      description: _descriptionController.text,
      rating: _rating,
      screenName: widget.screenName,
      appVersion: widget.appVersion,
      deviceModel: widget.deviceModel,
      osVersion: widget.osVersion,
    );

    final result = await widget.feedbackService.submitFeedback(submission);

    if (!mounted) return;

    setState(() {
      _isSubmitting = false;
    });

    switch (result) {
      case FeedbackSuccess():
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Thank you for your feedback!')),
        );
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

    return Scaffold(
      appBar: AppBar(
        title: const Text('Send Feedback'),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Category selector
            Text(
              'Category',
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
            ),
            SizedBox(height: tokens.space2),
            SegmentedButton<FeedbackCategory>(
              segments: const [
                ButtonSegment(
                  value: FeedbackCategory.bug,
                  label: Text('Bug'),
                  icon: Icon(Icons.bug_report),
                ),
                ButtonSegment(
                  value: FeedbackCategory.feature,
                  label: Text('Feature'),
                  icon: Icon(Icons.lightbulb),
                ),
                ButtonSegment(
                  value: FeedbackCategory.general,
                  label: Text('General'),
                  icon: Icon(Icons.chat),
                ),
              ],
              selected: {_selectedCategory},
              onSelectionChanged: (selected) {
                setState(() {
                  _selectedCategory = selected.first;
                });
              },
            ),
            SizedBox(height: tokens.space4),

            // Description
            Text(
              'Description',
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
            ),
            SizedBox(height: tokens.space2),
            TextField(
              controller: _descriptionController,
              maxLines: 5,
              maxLength: FeedbackSubmission.maxDescriptionLength,
              decoration: const InputDecoration(
                hintText: 'Tell us what you think...',
                border: OutlineInputBorder(),
              ),
            ),
            SizedBox(height: tokens.space4),

            // Rating
            Text(
              'Rating',
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
            ),
            SizedBox(height: tokens.space2),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(5, (index) {
                final starRating = index + 1;
                return IconButton(
                  icon: Icon(
                    starRating <= _rating ? Icons.star : Icons.star_border,
                    color: starRating <= _rating
                        ? Theme.of(context).colorScheme.primary
                        : tokens.contentMuted,
                    size: 36,
                  ),
                  onPressed: () {
                    setState(() {
                      _rating = starRating;
                    });
                  },
                );
              }),
            ),
            SizedBox(height: tokens.space4),

            // Error message
            if (_errorMessage != null)
              Padding(
                padding: EdgeInsets.only(bottom: tokens.space3),
                child: Text(
                  _errorMessage!,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: Theme.of(context).colorScheme.error,
                      ),
                ),
              ),

            // Submit button
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _isSubmitting ? null : _submit,
                child: _isSubmitting
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('Submit Feedback'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
