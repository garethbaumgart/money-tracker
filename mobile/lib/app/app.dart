import 'package:flutter/material.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

import 'shell/money_tracker_shell.dart';
import 'theme/app_theme_controller.dart';
import 'theme/app_theme_mode.dart';
import 'theme/money_tracker_theme.dart';

class MoneyTrackerApp extends StatefulWidget {
  const MoneyTrackerApp({
    super.key,
    this.themeController,
    this.themeMode = ThemeMode.system,
  });

  final AppThemeController? themeController;
  final ThemeMode themeMode;

  @override
  State<MoneyTrackerApp> createState() => _MoneyTrackerAppState();
}

class _MoneyTrackerAppState extends State<MoneyTrackerApp> {
  AppThemeController? _ownedController;

  AppThemeController get _controller =>
      widget.themeController ?? _ownedController!;

  @override
  void initState() {
    super.initState();
    _initializeOwnedController();
  }

  @override
  void didUpdateWidget(covariant MoneyTrackerApp oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.themeController == widget.themeController &&
        oldWidget.themeMode == widget.themeMode) {
      return;
    }

    if (widget.themeController != null) {
      _ownedController?.dispose();
      _ownedController = null;
      return;
    }

    _ownedController?.dispose();
    _initializeOwnedController();
  }

  @override
  void dispose() {
    _ownedController?.dispose();
    super.dispose();
  }

  void _initializeOwnedController() {
    if (widget.themeController != null) {
      _ownedController = null;
      return;
    }

    _ownedController = AppThemeController(
      initialMode: AppThemeMode.fromMaterialThemeMode(widget.themeMode),
      preferencesGateway: const NoopThemeModePreferencesGateway(),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AppThemeControllerScope(
      controller: _controller,
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) {
          return MaterialApp(
            title: 'Money Tracker',
            debugShowCheckedModeBanner: false,
            theme: MoneyTrackerTheme.light(),
            darkTheme: MoneyTrackerTheme.dark(),
            themeMode: _controller.materialThemeMode,
            routes: {
              MoneyTrackerShell.routeName: (context) =>
                  const MoneyTrackerShell(),
            },
            initialRoute: MoneyTrackerShell.routeName,
          );
        },
      ),
    );
  }
}
