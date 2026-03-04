import 'package:flutter/widgets.dart';
import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final preferencesGateway = SharedPreferencesThemeModePreferencesGateway();
  var initialMode = AppThemeMode.system;
  try {
    initialMode = await preferencesGateway.load();
  } catch (_) {
    initialMode = AppThemeMode.system;
  }
  final themeController = AppThemeController(
    initialMode: initialMode,
    preferencesGateway: preferencesGateway,
  );

  runApp(MoneyTrackerApp(themeController: themeController));
}
