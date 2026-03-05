import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/theme/app_theme_controller.dart';
import 'package:money_tracker/app/theme/app_theme_mode.dart';
import 'package:money_tracker/shared_kernel/preferences/theme_mode_preferences_gateway.dart';

void main() {
  group('AppThemeController', () {
    test('setMode keeps applied mode when persistence fails', () async {
      final controller = AppThemeController(
        initialMode: AppThemeMode.system,
        preferencesGateway: _ThrowingThemeModePreferencesGateway(),
      );
      addTearDown(controller.dispose);

      await expectLater(controller.setMode(AppThemeMode.dark), completes);
      expect(controller.mode, AppThemeMode.dark);
    });

    test(
      'setMode serializes persistence writes during rapid changes',
      () async {
        final gateway = _SequencedThemeModePreferencesGateway();
        final controller = AppThemeController(
          initialMode: AppThemeMode.system,
          preferencesGateway: gateway,
        );
        addTearDown(controller.dispose);

        final firstUpdate = controller.setMode(AppThemeMode.light);
        final secondUpdate = controller.setMode(AppThemeMode.dark);

        await Future<void>.delayed(Duration.zero);
        expect(gateway.startedSaves, <AppThemeMode>[AppThemeMode.light]);

        gateway.completeNextSave();
        await Future<void>.delayed(Duration.zero);
        expect(gateway.startedSaves, <AppThemeMode>[
          AppThemeMode.light,
          AppThemeMode.dark,
        ]);

        gateway.completeNextSave();
        await Future.wait<void>(<Future<void>>[firstUpdate, secondUpdate]);

        expect(gateway.completedSaves, <AppThemeMode>[
          AppThemeMode.light,
          AppThemeMode.dark,
        ]);
        expect(controller.mode, AppThemeMode.dark);
      },
    );
  });
}

class _ThrowingThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  @override
  Future<AppThemeMode> load() async {
    return AppThemeMode.system;
  }

  @override
  Future<void> save(AppThemeMode mode) {
    throw Exception('simulated write failure');
  }
}

class _SequencedThemeModePreferencesGateway
    implements ThemeModePreferencesGateway {
  final List<AppThemeMode> startedSaves = <AppThemeMode>[];
  final List<AppThemeMode> completedSaves = <AppThemeMode>[];
  final List<Completer<void>> _pendingSaves = <Completer<void>>[];

  @override
  Future<AppThemeMode> load() async {
    return AppThemeMode.system;
  }

  @override
  Future<void> save(AppThemeMode mode) {
    startedSaves.add(mode);
    final completer = Completer<void>();
    _pendingSaves.add(completer);

    return completer.future.then((_) {
      completedSaves.add(mode);
    });
  }

  void completeNextSave() {
    if (_pendingSaves.isEmpty) {
      throw StateError('No pending saves to complete.');
    }

    _pendingSaves.removeAt(0).complete();
  }
}
