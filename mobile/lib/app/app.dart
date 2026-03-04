import 'package:flutter/material.dart';

import 'shell/money_tracker_shell.dart';
import 'theme/money_tracker_theme.dart';

class MoneyTrackerApp extends StatelessWidget {
  const MoneyTrackerApp({super.key, this.themeMode = ThemeMode.system});

  final ThemeMode themeMode;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Money Tracker',
      debugShowCheckedModeBanner: false,
      theme: MoneyTrackerTheme.light(),
      darkTheme: MoneyTrackerTheme.dark(),
      themeMode: themeMode,
      routes: {
        MoneyTrackerShell.routeName: (context) => const MoneyTrackerShell(),
      },
      initialRoute: MoneyTrackerShell.routeName,
    );
  }
}
