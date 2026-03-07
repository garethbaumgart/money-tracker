import 'dart:async';

import 'package:flutter/material.dart';
import 'package:money_tracker/features/dashboard/dashboard_controller.dart';
import 'package:money_tracker/features/dashboard/dashboard_screen.dart';
import 'package:money_tracker/features/insights/application/insights_controller.dart';
import 'package:money_tracker/features/insights/presentation/insights_dashboard_screen.dart';
import 'package:money_tracker/features/reminders/reminders_controller.dart';
import 'package:money_tracker/features/reminders/reminders_screen.dart';

import '../theme/app_theme_controller.dart';
import '../theme/app_theme_mode.dart';
import '../theme/app_theme_tokens.dart';

const double _shellExpandedBreakpoint = 980.0;
const double _dashboardSplitBreakpoint = 900.0;
const double _summaryCompactBreakpoint = 560.0;
const double _metricsStackedBreakpoint = 420.0;

class MoneyTrackerShell extends StatefulWidget {
  const MoneyTrackerShell({super.key});

  static const routeName = '/';

  @override
  State<MoneyTrackerShell> createState() => _MoneyTrackerShellState();
}

class _MoneyTrackerShellState extends State<MoneyTrackerShell> {
  static const _destinations = <_ShellDestination>[
    _ShellDestination.home,
    _ShellDestination.insights,
    _ShellDestination.budgets,
    _ShellDestination.activity,
    _ShellDestination.household,
    _ShellDestination.settings,
  ];

  var _selectedIndex = 0;
  late final DashboardController _dashboardController;
  late final InsightsController _insightsController;
  late final RemindersController _remindersController;

  @override
  void initState() {
    super.initState();
    _dashboardController = DashboardController();
    _insightsController = InsightsController();
    _remindersController = RemindersController();
    assert(() {
      _dashboardController.seedSample();
      _insightsController.seedSample();
      _remindersController.seedSample();
      return true;
    }());
  }

  @override
  void dispose() {
    _dashboardController.dispose();
    _insightsController.dispose();
    _remindersController.dispose();
    super.dispose();
  }

  void _onDestinationSelected(int index) {
    if (_selectedIndex == index) {
      return;
    }

    setState(() {
      _selectedIndex = index;
    });
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isExpandedShell =
            constraints.maxWidth >= _shellExpandedBreakpoint;

        return Scaffold(
          appBar: AppBar(
            title: Text(
              _selectedIndex == 0
                  ? 'Wednesday plan'
                  : _destinations[_selectedIndex].label,
            ),
            actions: [
              IconButton(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => RemindersScreen(
                        controller: _remindersController,
                      ),
                    ),
                  );
                },
                icon: const Icon(Icons.notifications_outlined),
                tooltip: 'Bill reminders',
              ),
            ],
          ),
          body: isExpandedShell
              ? Row(
                  children: [
                    NavigationRail(
                      selectedIndex: _selectedIndex,
                      onDestinationSelected: _onDestinationSelected,
                      labelType: NavigationRailLabelType.all,
                      destinations: _destinations
                          .map(
                            (destination) => NavigationRailDestination(
                              icon: Icon(destination.icon),
                              selectedIcon: Icon(destination.selectedIcon),
                              label: Text(destination.label),
                            ),
                          )
                          .toList(growable: false),
                    ),
                    const VerticalDivider(width: 1),
                    Expanded(
                      child: _ShellBody(
                        destination: _destinations[_selectedIndex],
                        dashboardController: _dashboardController,
                        insightsController: _insightsController,
                      ),
                    ),
                  ],
                )
              : _ShellBody(
                  destination: _destinations[_selectedIndex],
                  dashboardController: _dashboardController,
                  insightsController: _insightsController,
                ),
          bottomNavigationBar: isExpandedShell
              ? null
              : NavigationBar(
                  selectedIndex: _selectedIndex,
                  onDestinationSelected: _onDestinationSelected,
                  destinations: _destinations
                      .map(
                        (destination) => NavigationDestination(
                          icon: Icon(destination.icon),
                          selectedIcon: Icon(destination.selectedIcon),
                          label: destination.label,
                        ),
                      )
                      .toList(growable: false),
                ),
        );
      },
    );
  }
}

class _ShellBody extends StatelessWidget {
  const _ShellBody({
    required this.destination,
    required this.dashboardController,
    required this.insightsController,
  });

  final _ShellDestination destination;
  final DashboardController dashboardController;
  final InsightsController insightsController;

  @override
  Widget build(BuildContext context) {
    if (destination == _ShellDestination.home) {
      return DashboardScreen(controller: dashboardController);
    }
    if (destination == _ShellDestination.insights) {
      return InsightsDashboardScreen(controller: insightsController);
    }
    if (destination == _ShellDestination.settings) {
      return const _SettingsView();
    }

    final tokens = AppThemeTokens.of(context);

    return Padding(
      padding: EdgeInsets.all(tokens.space4),
      child: Card(
        child: Padding(
          padding: EdgeInsets.all(tokens.space5),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                destination.label,
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              SizedBox(height: tokens.space2),
              Text(
                '${destination.label} foundation content will be built in a dedicated slice.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: tokens.contentSecondary,
                ),
              ),
              const Spacer(),
              TextButton.icon(
                onPressed: () {},
                icon: const Icon(Icons.open_in_new),
                label: const Text('Explore roadmap'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SettingsView extends StatelessWidget {
  const _SettingsView();

  @override
  Widget build(BuildContext context) {
    final themeController = AppThemeControllerScope.of(context);
    final tokens = AppThemeTokens.of(context);
    final currentMode = themeController.mode;

    return SingleChildScrollView(
      padding: EdgeInsets.all(tokens.space4),
      child: Card(
        child: Padding(
          padding: EdgeInsets.all(tokens.space4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Settings',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              SizedBox(height: tokens.space2),
              Text('Theme', style: Theme.of(context).textTheme.titleMedium),
              SizedBox(height: tokens.space1),
              Text(
                'Choose how the app appearance is resolved.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: tokens.contentSecondary,
                ),
              ),
              SizedBox(height: tokens.space3),
              SegmentedButton<AppThemeMode>(
                showSelectedIcon: false,
                segments: AppThemeMode.values
                    .map(
                      (mode) => ButtonSegment<AppThemeMode>(
                        value: mode,
                        label: Text(mode.label),
                      ),
                    )
                    .toList(growable: false),
                selected: <AppThemeMode>{currentMode},
                onSelectionChanged: (selection) {
                  final selectedMode = selection.first;
                  unawaited(themeController.setMode(selectedMode));
                },
              ),
              SizedBox(height: tokens.space2),
              Text(
                switch (currentMode) {
                  AppThemeMode.system =>
                    'Following your device light/dark preference.',
                  AppThemeMode.light => 'Light mode is forced for this app.',
                  AppThemeMode.dark => 'Dark mode is forced for this app.',
                },
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: tokens.contentMuted),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ignore: unused_element
class _HomeDashboard extends StatelessWidget {
  const _HomeDashboard();

  @override
  Widget build(BuildContext context) {
    final tokens = AppThemeTokens.of(context);

    return LayoutBuilder(
      builder: (context, constraints) {
        final showSplitOverview =
            constraints.maxWidth >= _dashboardSplitBreakpoint;

        return SingleChildScrollView(
          padding: EdgeInsets.all(tokens.space4),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _TopSummaryCard(tokens: tokens),
              SizedBox(height: tokens.space4),
              _ResponsivePair(
                showSplitOverview: showSplitOverview,
                tokens: tokens,
                leftFlex: 3,
                rightFlex: 2,
                left: _ForecastCard(tokens: tokens),
                right: _PriorityChecklistCard(tokens: tokens),
              ),
              SizedBox(height: tokens.space3),
              _ResponsivePair(
                showSplitOverview: showSplitOverview,
                tokens: tokens,
                left: _RecentActivityCard(tokens: tokens),
                right: _UiStatesCard(tokens: tokens),
              ),
              SizedBox(height: tokens.space3),
              _ResponsivePair(
                showSplitOverview: showSplitOverview,
                tokens: tokens,
                left: _ResponsiveRuleCard(tokens: tokens),
                right: _AccessibilityCard(tokens: tokens),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ResponsivePair extends StatelessWidget {
  const _ResponsivePair({
    required this.showSplitOverview,
    required this.tokens,
    required this.left,
    required this.right,
    this.leftFlex = 1,
    this.rightFlex = 1,
  });

  final bool showSplitOverview;
  final AppThemeTokens tokens;
  final Widget left;
  final Widget right;
  final int leftFlex;
  final int rightFlex;

  @override
  Widget build(BuildContext context) {
    if (showSplitOverview) {
      return Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(flex: leftFlex, child: left),
          SizedBox(width: tokens.space3),
          Expanded(flex: rightFlex, child: right),
        ],
      );
    }

    return Column(
      children: [
        left,
        SizedBox(height: tokens.space3),
        right,
      ],
    );
  }
}

class _TopSummaryCard extends StatelessWidget {
  const _TopSummaryCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Container(
        padding: EdgeInsets.all(tokens.space4),
        color: tokens.surfaceMuted,
        child: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < _summaryCompactBreakpoint;

            final header = Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Northside Household',
                  style: Theme.of(
                    context,
                  ).textTheme.labelMedium?.copyWith(color: tokens.contentMuted),
                ),
                SizedBox(height: tokens.space1),
                Text(
                  'Wednesday plan',
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ],
            );

            final actions = Wrap(
              spacing: tokens.space2,
              runSpacing: tokens.space2,
              children: [
                OutlinedButton(
                  onPressed: () {},
                  child: const Text('Switch household'),
                ),
                FilledButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.add),
                  label: const Text('Add transaction'),
                ),
              ],
            );

            if (compact) {
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  header,
                  SizedBox(height: tokens.space3),
                  actions,
                ],
              );
            }

            return Row(
              children: [
                Expanded(child: header),
                actions,
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ForecastCard extends StatelessWidget {
  const _ForecastCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      decoration: BoxDecoration(
        borderRadius: tokens.radiusMedium,
        border: Border.all(color: tokens.borderSubtle),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            Color.lerp(scheme.primary, tokens.surfaceElevated, 0.78)!,
            tokens.surfaceElevated,
          ],
        ),
      ),
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Forecast confidence',
              style: Theme.of(
                context,
              ).textTheme.labelLarge?.copyWith(color: tokens.contentSecondary),
            ),
            SizedBox(height: tokens.space1),
            Text(
              'Finish this cycle with a \$290 buffer.',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            SizedBox(height: tokens.space2),
            Text(
              'Dining and fuel are the only categories trending above baseline.',
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: tokens.contentSecondary),
            ),
            SizedBox(height: tokens.space3),
            _MetricRow(tokens: tokens),
          ],
        ),
      ),
    );
  }
}

class _MetricRow extends StatelessWidget {
  const _MetricRow({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    final metrics = const [
      _Metric('Projected spend', '\$3,210'),
      _Metric('Cycle target', '\$3,500'),
      _Metric('Days remaining', '16'),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final stacked = constraints.maxWidth < _metricsStackedBreakpoint;

        if (stacked) {
          return Column(
            children: metrics
                .map(
                  (metric) => Padding(
                    padding: EdgeInsets.only(bottom: tokens.space2),
                    child: _MetricCard(tokens: tokens, metric: metric),
                  ),
                )
                .toList(growable: false),
          );
        }

        return Row(
          children: metrics
              .map(
                (metric) => Expanded(
                  child: Padding(
                    padding: EdgeInsets.only(
                      right: metric == metrics.last ? 0 : tokens.space2,
                    ),
                    child: _MetricCard(tokens: tokens, metric: metric),
                  ),
                ),
              )
              .toList(growable: false),
        );
      },
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({required this.tokens, required this.metric});

  final AppThemeTokens tokens;
  final _Metric metric;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space3),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              metric.label,
              style: Theme.of(
                context,
              ).textTheme.labelMedium?.copyWith(color: tokens.contentMuted),
            ),
            SizedBox(height: tokens.space2),
            Text(metric.value, style: Theme.of(context).textTheme.titleLarge),
          ],
        ),
      ),
    );
  }
}

class _PriorityChecklistCard extends StatelessWidget {
  const _PriorityChecklistCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Priority checklist',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space3),
            _ChecklistTile(
              title: 'Review dining cap',
              subtitle: 'At 83% with 16 days left.',
              tokens: tokens,
            ),
            SizedBox(height: tokens.space2),
            _ChecklistTile(
              title: 'Confirm transfer with partner',
              subtitle: 'Split utility payment due tonight.',
              tokens: tokens,
            ),
            SizedBox(height: tokens.space2),
            _ChecklistTile(
              title: 'Categorize uncategorized items',
              subtitle: '4 transactions pending.',
              tokens: tokens,
            ),
            SizedBox(height: tokens.space3),
            const TextField(
              decoration: InputDecoration(
                labelText: 'Quick reminder',
                hintText: 'Add a short note for this cycle',
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ChecklistTile extends StatelessWidget {
  const _ChecklistTile({
    required this.title,
    required this.subtitle,
    required this.tokens,
  });

  final String title;
  final String subtitle;
  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border.all(color: tokens.borderSubtle),
        borderRadius: tokens.radiusSmall,
      ),
      child: Padding(
        padding: EdgeInsets.all(tokens.space3),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleSmall),
            SizedBox(height: tokens.space1),
            Text(
              subtitle,
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: tokens.contentSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class _RecentActivityCard extends StatelessWidget {
  const _RecentActivityCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
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
                    'Recent activity',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                TextButton(onPressed: () {}, child: const Text('View all')),
              ],
            ),
            SizedBox(height: tokens.space2),
            _ActivityRow(
              tokens: tokens,
              label: 'Green Market groceries',
              value: '-\$97.50',
            ),
            SizedBox(height: tokens.space2),
            _ActivityRow(
              tokens: tokens,
              label: 'City Servo fuel',
              value: '-\$66.10',
            ),
            SizedBox(height: tokens.space2),
            _ActivityRow(
              tokens: tokens,
              label: 'Salary',
              value: '+\$2,100',
              badgeColor: tokens.stateSuccess,
            ),
            SizedBox(height: tokens.space2),
            _ActivityRow(
              tokens: tokens,
              label: 'Streaming plan renewal',
              value: 'Review',
              badgeColor: tokens.stateWarning,
            ),
          ],
        ),
      ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  const _ActivityRow({
    required this.tokens,
    required this.label,
    required this.value,
    this.badgeColor,
  });

  final AppThemeTokens tokens;
  final String label;
  final String value;
  final Color? badgeColor;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border.all(color: tokens.borderSubtle),
        borderRadius: tokens.radiusSmall,
      ),
      child: Padding(
        padding: EdgeInsets.all(tokens.space3),
        child: Row(
          children: [
            Expanded(child: Text(label)),
            if (badgeColor == null)
              Text(value, style: Theme.of(context).textTheme.titleSmall)
            else
              _StatusBadge(text: value, color: badgeColor!),
          ],
        ),
      ),
    );
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
        color: Color.lerp(Theme.of(context).colorScheme.surface, color, 0.18),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        child: Text(
          text,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
            color: color,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}

class _UiStatesCard extends StatelessWidget {
  const _UiStatesCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('UI states', style: Theme.of(context).textTheme.titleMedium),
            SizedBox(height: tokens.space3),
            _StateMessage(
              tokens: tokens,
              title: 'Loading',
              message: 'Refreshing confidence forecast.',
            ),
            SizedBox(height: tokens.space2),
            _StateMessage(
              tokens: tokens,
              title: 'Empty',
              message: 'No reminders for today.',
            ),
            SizedBox(height: tokens.space2),
            _StateMessage(
              tokens: tokens,
              title: 'Error',
              message: 'Could not load one bank account snapshot.',
              borderColor: tokens.stateDanger,
            ),
          ],
        ),
      ),
    );
  }
}

class _StateMessage extends StatelessWidget {
  const _StateMessage({
    required this.tokens,
    required this.title,
    required this.message,
    this.borderColor,
  });

  final AppThemeTokens tokens;
  final String title;
  final String message;
  final Color? borderColor;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: tokens.surfaceMuted,
        borderRadius: tokens.radiusSmall,
        border: Border.all(color: borderColor ?? tokens.borderSubtle),
      ),
      child: Padding(
        padding: EdgeInsets.all(tokens.space3),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleSmall),
            SizedBox(height: tokens.space1),
            Text(
              message,
              style: Theme.of(
                context,
              ).textTheme.bodySmall?.copyWith(color: tokens.contentSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class _ResponsiveRuleCard extends StatelessWidget {
  const _ResponsiveRuleCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Responsive rule',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space2),
            Text(
              'Compact widths stack checklist below forecast. Expanded widths keep checklist visible beside forecast for scan speed.',
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: tokens.contentSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class _AccessibilityCard extends StatelessWidget {
  const _AccessibilityCard({required this.tokens});

  final AppThemeTokens tokens;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: EdgeInsets.all(tokens.space4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Accessibility note',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            SizedBox(height: tokens.space2),
            Text(
              'Strong contrast surfaces and clear status labels support color-agnostic interpretation of alerts and success states.',
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: tokens.contentSecondary),
            ),
          ],
        ),
      ),
    );
  }
}

class _Metric {
  const _Metric(this.label, this.value);

  final String label;
  final String value;
}

enum _ShellDestination {
  home(label: 'Home', icon: Icons.home_outlined, selectedIcon: Icons.home),
  insights(
    label: 'Insights',
    icon: Icons.insights_outlined,
    selectedIcon: Icons.insights,
  ),
  budgets(
    label: 'Budgets',
    icon: Icons.savings_outlined,
    selectedIcon: Icons.savings,
  ),
  activity(
    label: 'Activity',
    icon: Icons.receipt_long_outlined,
    selectedIcon: Icons.receipt_long,
  ),
  household(
    label: 'Household',
    icon: Icons.people_outline,
    selectedIcon: Icons.people,
  ),
  settings(
    label: 'Settings',
    icon: Icons.settings_outlined,
    selectedIcon: Icons.settings,
  );

  const _ShellDestination({
    required this.label,
    required this.icon,
    required this.selectedIcon,
  });

  final String label;
  final IconData icon;
  final IconData selectedIcon;
}
