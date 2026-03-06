enum AppEnvironment { local, staging, production }

class AppConfigException implements Exception {
  AppConfigException(this.message);

  final String message;

  @override
  String toString() => 'AppConfigException: $message';
}

class AppConfig {
  AppConfig._({
    required this.environment,
    required this.apiBaseUrl,
    required this.errorReportingDsn,
  });

  final AppEnvironment environment;
  final Uri apiBaseUrl;
  final String? errorReportingDsn;

  factory AppConfig.fromEnvironment() {
    return AppConfig.fromRaw(
      appEnv: const String.fromEnvironment('APP_ENV', defaultValue: ''),
      apiBaseUrl: const String.fromEnvironment('API_BASE_URL', defaultValue: ''),
      errorReportingDsn: const String.fromEnvironment(
        'ERROR_REPORTING_DSN',
        defaultValue: '',
      ),
    );
  }

  factory AppConfig.fromRaw({
    required String appEnv,
    required String apiBaseUrl,
    String? errorReportingDsn,
  }) {
    final normalizedEnv = appEnv.trim().toLowerCase();
    final environment = switch (normalizedEnv) {
      'local' => AppEnvironment.local,
      'staging' => AppEnvironment.staging,
      'production' => AppEnvironment.production,
      _ => throw AppConfigException(
        'APP_ENV is required and must be one of: local, staging, production.',
      ),
    };

    final normalizedBaseUrl = apiBaseUrl.trim();
    if (normalizedBaseUrl.isEmpty) {
      throw AppConfigException('API_BASE_URL is required.');
    }

    final parsedBaseUrl = Uri.tryParse(normalizedBaseUrl);
    if (parsedBaseUrl == null || !parsedBaseUrl.hasScheme || parsedBaseUrl.host.isEmpty) {
      throw AppConfigException('API_BASE_URL must be an absolute URL.');
    }

    if (parsedBaseUrl.scheme != 'http' && parsedBaseUrl.scheme != 'https') {
      throw AppConfigException('API_BASE_URL must use http or https.');
    }

    final normalizedDsn = errorReportingDsn?.trim();

    return AppConfig._(
      environment: environment,
      apiBaseUrl: parsedBaseUrl,
      errorReportingDsn: normalizedDsn == null || normalizedDsn.isEmpty
          ? null
          : normalizedDsn,
    );
  }
}
