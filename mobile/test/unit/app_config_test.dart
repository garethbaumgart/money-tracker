import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/app/config/app_config.dart';

void main() {
  test('parses valid raw configuration values', () {
    final config = AppConfig.fromRaw(
      appEnv: 'local',
      apiBaseUrl: 'https://api.local.money-tracker.test',
      errorReportingDsn: '  dsn://abc  ',
    );

    expect(config.environment, AppEnvironment.local);
    expect(config.apiBaseUrl.toString(), 'https://api.local.money-tracker.test');
    expect(config.errorReportingDsn, 'dsn://abc');
  });

  test('throws when APP_ENV is missing or unsupported', () {
    expect(
      () => AppConfig.fromRaw(
        appEnv: '',
        apiBaseUrl: 'https://api.local.money-tracker.test',
      ),
      throwsA(
        isA<AppConfigException>().having(
          (exception) => exception.message,
          'message',
          contains('APP_ENV is required'),
        ),
      ),
    );
  });

  test('throws when API_BASE_URL is missing', () {
    expect(
      () => AppConfig.fromRaw(appEnv: 'production', apiBaseUrl: '   '),
      throwsA(
        isA<AppConfigException>().having(
          (exception) => exception.message,
          'message',
          contains('API_BASE_URL is required'),
        ),
      ),
    );
  });

  test('throws when API_BASE_URL is not absolute', () {
    expect(
      () => AppConfig.fromRaw(appEnv: 'staging', apiBaseUrl: '/relative/path'),
      throwsA(
        isA<AppConfigException>().having(
          (exception) => exception.message,
          'message',
          contains('absolute URL'),
        ),
      ),
    );
  });
}
