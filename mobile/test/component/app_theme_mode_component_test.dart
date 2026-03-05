import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

void main() {
  group('settings theme picker', () {
    testWidgets('shows exactly System, Light, and Dark options', (
      WidgetTester tester,
    ) async {
      final gateway = _FakeThemeModePreferencesGateway();
      final controller = AppThemeController(
        initialMode: AppThemeMode.light,
        preferencesGateway: gateway,
      );
      addTearDown(controller.dispose);

      tester.view.physicalSize = const Size(1280, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(MoneyTrackerApp(themeController: controller));
      await tester.pumpAndSettle();

      await tester.tap(
        find.descendant(
          of: find.byType(NavigationRail),
          matching: find.text('Settings'),
        ),
      );
      await tester.pumpAndSettle();

      final segmentedButton = tester.widget<SegmentedButton<AppThemeMode>>(
        find.byType(SegmentedButton<AppThemeMode>),
      );
      final segmentValues = segmentedButton.segments
          .map((segment) => segment.value)
          .toList(growable: false);

      expect(segmentValues, AppThemeMode.values);
      expect(segmentedButton.selected, <AppThemeMode>{AppThemeMode.light});
    });

    testWidgets('applies and persists selected mode immediately', (
      WidgetTester tester,
    ) async {
      final gateway = _FakeThemeModePreferencesGateway();
      final controller = AppThemeController(
        initialMode: AppThemeMode.system,
        preferencesGateway: gateway,
      );
      addTearDown(controller.dispose);

      tester.view.physicalSize = const Size(1280, 900);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(MoneyTrackerApp(themeController: controller));
      await tester.pumpAndSettle();

      await tester.tap(
        find.descendant(
          of: find.byType(NavigationRail),
          matching: find.text('Settings'),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Dark'));
      await tester.pumpAndSettle();

      expect(controller.mode, AppThemeMode.dark);
      expect(gateway.savedModes, <AppThemeMode>[AppThemeMode.dark]);
    });
  });

  group('theme mode behavior', () {
    testWidgets('System mode follows platform brightness changes', (
      WidgetTester tester,
    ) async {
      final gateway = _FakeThemeModePreferencesGateway();
      final controller = AppThemeController(
        initialMode: AppThemeMode.system,
        preferencesGateway: gateway,
      );
      addTearDown(controller.dispose);

      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      tester.binding.platformDispatcher.platformBrightnessTestValue =
          Brightness.light;
      addTearDown(
        tester.binding.platformDispatcher.clearPlatformBrightnessTestValue,
      );

      await tester.pumpWidget(MoneyTrackerApp(themeController: controller));
      await tester.pumpAndSettle();
      expect(_effectiveBrightness(tester), Brightness.light);

      tester.binding.platformDispatcher.platformBrightnessTestValue =
          Brightness.dark;
      tester.binding.handlePlatformBrightnessChanged();
      await tester.pumpAndSettle();

      expect(_effectiveBrightness(tester), Brightness.dark);
    });

    testWidgets('forced mode ignores platform brightness changes', (
      WidgetTester tester,
    ) async {
      final gateway = _FakeThemeModePreferencesGateway();
      final controller = AppThemeController(
        initialMode: AppThemeMode.light,
        preferencesGateway: gateway,
      );
      addTearDown(controller.dispose);

      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      tester.binding.platformDispatcher.platformBrightnessTestValue =
          Brightness.dark;
      addTearDown(
        tester.binding.platformDispatcher.clearPlatformBrightnessTestValue,
      );

      await tester.pumpWidget(MoneyTrackerApp(themeController: controller));
      await tester.pumpAndSettle();
      expect(_effectiveBrightness(tester), Brightness.light);

      tester.binding.platformDispatcher.platformBrightnessTestValue =
          Brightness.light;
      tester.binding.handlePlatformBrightnessChanged();
      await tester.pumpAndSettle();

      expect(_effectiveBrightness(tester), Brightness.light);
    });
  });
}

Brightness _effectiveBrightness(WidgetTester tester) {
  final context = tester.element(find.byType(Scaffold).first);
  return Theme.of(context).brightness;
}

class _FakeThemeModePreferencesGateway implements ThemeModePreferencesGateway {
  final List<AppThemeMode> savedModes = <AppThemeMode>[];

  @override
  Future<AppThemeMode> load() async {
    return AppThemeMode.system;
  }

  @override
  Future<void> save(AppThemeMode mode) async {
    savedModes.add(mode);
  }
}
