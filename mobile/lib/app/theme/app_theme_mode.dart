import 'package:flutter/material.dart';

enum AppThemeMode {
  system(storageValue: 'system', label: 'System'),
  light(storageValue: 'light', label: 'Light'),
  dark(storageValue: 'dark', label: 'Dark');

  const AppThemeMode({required this.storageValue, required this.label});

  final String storageValue;
  final String label;

  ThemeMode toMaterialThemeMode() {
    return switch (this) {
      AppThemeMode.system => ThemeMode.system,
      AppThemeMode.light => ThemeMode.light,
      AppThemeMode.dark => ThemeMode.dark,
    };
  }

  static AppThemeMode fromMaterialThemeMode(ThemeMode mode) {
    return switch (mode) {
      ThemeMode.system => AppThemeMode.system,
      ThemeMode.light => AppThemeMode.light,
      ThemeMode.dark => AppThemeMode.dark,
    };
  }
}

final class AppThemeModeResolver {
  const AppThemeModeResolver();

  AppThemeMode resolve(String? storedValue) {
    switch (storedValue?.trim().toLowerCase()) {
      case 'light':
        return AppThemeMode.light;
      case 'dark':
        return AppThemeMode.dark;
      case 'system':
      default:
        return AppThemeMode.system;
    }
  }
}
