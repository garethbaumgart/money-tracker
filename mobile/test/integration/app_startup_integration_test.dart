import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/app/app.dart';
import 'package:money_tracker/app/config/app_config.dart';

void main() {
  testWidgets('boots app and keeps shell stable across destinations', (
    WidgetTester tester,
  ) async {
    final appConfig = AppConfig.fromRaw(
      appEnv: 'local',
      apiBaseUrl: 'https://api.local.money-tracker.test',
    );
    expect(appConfig.environment, AppEnvironment.local);

    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const MoneyTrackerApp(themeMode: ThemeMode.system));
    await tester.pumpAndSettle();

    expect(find.text('Forecast confidence'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);

    await tester.tap(find.text('Activity'));
    await tester.pumpAndSettle();
    expect(
      find.text(
        'Activity foundation content will be built in a dedicated slice.',
      ),
      findsOneWidget,
    );

    await tester.tap(find.text('Home'));
    await tester.pumpAndSettle();
    expect(find.text('Priority checklist'), findsOneWidget);
  });

  test('fails fast when startup config is invalid', () {
    expect(
      () => AppConfig.fromRaw(appEnv: 'local', apiBaseUrl: ''),
      throwsA(
        isA<AppConfigException>().having(
          (exception) => exception.message,
          'message',
          contains('API_BASE_URL is required'),
        ),
      ),
    );
  });
}
