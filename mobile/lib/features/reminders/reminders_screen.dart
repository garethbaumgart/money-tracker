import 'package:flutter/material.dart';
import 'package:money_tracker/app/theme/app_theme_tokens.dart';

import 'reminders_controller.dart';

class RemindersScreen extends StatelessWidget {
  const RemindersScreen({super.key, required this.controller});

  final RemindersController controller;

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Bill reminders'),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showAddReminder(context),
        icon: const Icon(Icons.add),
        label: const Text('Add reminder'),
      ),
      body: AnimatedBuilder(
        animation: controller,
        builder: (context, _) {
          return ListView(
            padding: EdgeInsets.all(tokens.space4),
            children: [
              _NotificationStatusCard(controller: controller, tokens: tokens),
              SizedBox(height: tokens.space3),
              if (controller.reminders.isEmpty)
                _EmptyState(tokens: tokens)
              else
                ...controller.reminders.map(
                  (reminder) => Padding(
                    padding: EdgeInsets.only(bottom: tokens.space3),
                    child: _ReminderCard(
                      reminder: reminder,
                      tokens: tokens,
                    ),
                  ),
                ),
              SizedBox(height: tokens.space6),
            ],
          );
        },
      ),
    );
  }

  Future<void> _showAddReminder(BuildContext context) async {
    final reminder = await showModalBottomSheet<ReminderEntry>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _AddReminderSheet(),
    );

    if (reminder != null) {
      controller.addReminder(reminder);
    }
  }
}

class _NotificationStatusCard extends StatelessWidget {
  const _NotificationStatusCard({
    required this.controller,
    required this.tokens,
  });

  final RemindersController controller;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Row(
          children: [
            Icon(
              controller.notificationsEnabled
                  ? Icons.notifications_active
                  : Icons.notifications_off_outlined,
              color: controller.notificationsEnabled
                  ? tokens.stateSuccess
                  : tokens.contentMuted,
            ),
            SizedBox(width: tokens.space3),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    controller.notificationsEnabled
                        ? 'Notifications connected'
                        : 'Notifications paused',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  SizedBox(height: tokens.space1),
                  Text(
                    controller.notificationsEnabled
                        ? 'Device token is ready for reminder dispatch.'
                        : 'Enable device tokens to receive bill alerts.',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: tokens.contentSecondary,
                    ),
                  ),
                ],
              ),
            ),
            Switch(
              value: controller.notificationsEnabled,
              onChanged: controller.toggleNotifications,
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('No reminders yet', style: Theme.of(context).textTheme.titleMedium),
            SizedBox(height: tokens.space1),
            Text(
              'Add upcoming bills to keep the household on the same page.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ReminderCard extends StatelessWidget {
  const _ReminderCard({
    required this.reminder,
    required this.tokens,
  });

  final ReminderEntry reminder;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    final dueLabel = _formatDate(context, reminder.dueDate);
    final badgeColor = reminder.isOverdue ? tokens.stateDanger : tokens.stateSuccess;
    final badgeText = reminder.isOverdue ? 'Overdue' : 'Due $dueLabel';

    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    reminder.title,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                _StatusBadge(text: badgeText, color: badgeColor),
              ],
            ),
            SizedBox(height: tokens.space2),
            Text(
              '\$${reminder.amount.toStringAsFixed(2)} • ${reminder.cadence}',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: tokens.contentSecondary,
              ),
            ),
            if (reminder.lastDispatchErrorCode != null) ...[
              SizedBox(height: tokens.space2),
              Text(
                'Last dispatch: ${reminder.lastDispatchErrorMessage ?? reminder.lastDispatchErrorCode}',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: tokens.stateDanger,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatDate(BuildContext context, DateTime date) {
    return MaterialLocalizations.of(context).formatMediumDate(date);
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.text, required this.color});

  final String text;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Color.lerp(Theme.of(context).colorScheme.surface, color, 0.15),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
        child: Text(
          text,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
            color: color,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }
}

class _AddReminderSheet extends StatefulWidget {
  const _AddReminderSheet();

  @override
  State<_AddReminderSheet> createState() => _AddReminderSheetState();
}

class _AddReminderSheetState extends State<_AddReminderSheet> {
  final _titleController = TextEditingController();
  final _amountController = TextEditingController();
  DateTime _dueDate = DateTime.now().add(const Duration(days: 3));
  String _cadence = 'Monthly';

  @override
  void dispose() {
    _titleController.dispose();
    _amountController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return Padding(
      padding: EdgeInsets.only(
        left: tokens.space4,
        right: tokens.space4,
        top: tokens.space4,
        bottom: MediaQuery.of(context).viewInsets.bottom + tokens.space4,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('New reminder', style: Theme.of(context).textTheme.titleLarge),
          SizedBox(height: tokens.space3),
          TextField(
            controller: _titleController,
            decoration: const InputDecoration(labelText: 'Title'),
          ),
          SizedBox(height: tokens.space2),
          TextField(
            controller: _amountController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Amount'),
          ),
          SizedBox(height: tokens.space2),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: _pickDate,
                  icon: const Icon(Icons.calendar_today_outlined),
                  label: Text(_formatDate(context, _dueDate)),
                ),
              ),
              SizedBox(width: tokens.space2),
              DropdownButton<String>(
                value: _cadence,
                items: const [
                  DropdownMenuItem(value: 'Once', child: Text('Once')),
                  DropdownMenuItem(value: 'Weekly', child: Text('Weekly')),
                  DropdownMenuItem(value: 'BiWeekly', child: Text('Biweekly')),
                  DropdownMenuItem(value: 'Monthly', child: Text('Monthly')),
                ],
                onChanged: (value) {
                  if (value == null) return;
                  setState(() {
                    _cadence = value;
                  });
                },
              ),
            ],
          ),
          SizedBox(height: tokens.space3),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: const Text('Cancel'),
                ),
              ),
              SizedBox(width: tokens.space2),
              Expanded(
                child: FilledButton(
                  onPressed: _submit,
                  child: const Text('Save'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _pickDate() async {
    final selected = await showDatePicker(
      context: context,
      initialDate: _dueDate,
      firstDate: DateTime.now().subtract(const Duration(days: 30)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );

    if (selected != null) {
      setState(() {
        _dueDate = selected;
      });
    }
  }

  void _submit() {
    final title = _titleController.text.trim();
    final amount = double.tryParse(_amountController.text.trim());
    if (title.isEmpty || amount == null || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enter a title and valid amount.')),
      );
      return;
    }

    final reminder = ReminderEntry(
      id: DateTime.now().microsecondsSinceEpoch.toString(),
      title: title,
      amount: amount,
      dueDate: _dueDate,
      cadence: _cadence,
    );
    Navigator.of(context).pop(reminder);
  }

  String _formatDate(BuildContext context, DateTime date) {
    return MaterialLocalizations.of(context).formatMediumDate(date);
  }
}
