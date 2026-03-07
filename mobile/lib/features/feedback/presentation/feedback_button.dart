import 'package:flutter/material.dart';
import '../application/feedback_service.dart';
import 'feedback_form_screen.dart';

/// A button widget for settings integration that navigates to the feedback form.
/// AC-11: Feedback form accessible from settings.
class FeedbackButton extends StatelessWidget {
  const FeedbackButton({
    super.key,
    required this.feedbackService,
    this.appVersion,
    this.deviceModel,
    this.osVersion,
  });

  final FeedbackService feedbackService;
  final String? appVersion;
  final String? deviceModel;
  final String? osVersion;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: const Icon(Icons.feedback_outlined),
      title: const Text('Send Feedback'),
      subtitle: const Text('Report bugs or request features'),
      trailing: const Icon(Icons.chevron_right),
      onTap: () {
        FeedbackFormScreen.navigate(
          context,
          feedbackService: feedbackService,
          screenName: 'settings',
          appVersion: appVersion,
          deviceModel: deviceModel,
          osVersion: osVersion,
        );
      },
    );
  }
}
