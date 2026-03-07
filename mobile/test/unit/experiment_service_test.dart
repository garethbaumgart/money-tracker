import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/experiments/application/experiment_service.dart';
import 'package:money_tracker/features/experiments/infrastructure/experiment_api_client.dart';
import 'package:money_tracker/features/subscriptions/infrastructure/subscription_gateway.dart';

class StubHttpClient implements HttpClientAdapter {
  StubHttpClient({
    required this.statusCode,
    required this.responseBody,
  });

  final int statusCode;
  final String responseBody;
  int callCount = 0;

  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    callCount++;
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    callCount++;
    return HttpResponse(statusCode: statusCode, body: responseBody);
  }
}

class FailingHttpClient implements HttpClientAdapter {
  @override
  Future<HttpResponse> get(Uri uri, {Map<String, String>? headers}) async {
    return HttpResponse(statusCode: 500, body: 'Internal Server Error');
  }

  @override
  Future<HttpResponse> post(Uri uri,
      {String? body, Map<String, String>? headers}) async {
    return HttpResponse(statusCode: 500, body: 'Internal Server Error');
  }
}

void main() {
  late StubHttpClient httpClient;
  late ExperimentApiClient apiClient;
  late ExperimentService service;

  final allocationsResponse = jsonEncode({
    'allocations': [
      {
        'experimentId': '11111111-1111-1111-1111-111111111111',
        'experimentName': 'Onboarding Flow',
        'variantName': 'Treatment',
        'allocatedAtUtc': '2026-03-01T00:00:00Z',
      },
      {
        'experimentId': '22222222-2222-2222-2222-222222222222',
        'experimentName': 'Paywall Test',
        'variantName': 'Control',
        'allocatedAtUtc': '2026-03-02T00:00:00Z',
      },
    ],
  });

  setUp(() {
    httpClient = StubHttpClient(
      statusCode: 200,
      responseBody: allocationsResponse,
    );
    apiClient = ExperimentApiClient(
      apiBaseUrl: Uri.parse('https://api.example.com'),
      tokenProvider: () => 'test-token',
      httpClient: httpClient,
    );
    service = ExperimentService(
      apiClient: apiClient,
      cacheTtl: const Duration(minutes: 5),
    );
  });

  group('ExperimentService', () {
    test('starts with empty allocations', () {
      expect(service.allocations, isEmpty);
      expect(service.getVariant('Onboarding Flow'), isNull);
    });

    test('fetchAllocations returns allocations from API', () async {
      final result = await service.fetchAllocations();

      expect(result.length, 2);
      expect(result[0].experimentName, 'Onboarding Flow');
      expect(result[0].variantName, 'Treatment');
      expect(result[1].experimentName, 'Paywall Test');
      expect(result[1].variantName, 'Control');
      expect(httpClient.callCount, 1);
    });

    test('cache hit within TTL does not make API call', () async {
      await service.fetchAllocations();
      expect(httpClient.callCount, 1);

      // Second call should use cache
      final result = await service.fetchAllocations();
      expect(httpClient.callCount, 1);
      expect(result.length, 2);
    });

    test('invalidateCache forces fresh API call', () async {
      await service.fetchAllocations();
      expect(httpClient.callCount, 1);

      service.invalidateCache();

      await service.fetchAllocations();
      expect(httpClient.callCount, 2);
    });

    test('getVariant returns correct variant for experiment', () async {
      await service.fetchAllocations();

      expect(service.getVariant('Onboarding Flow'), 'Treatment');
      expect(service.getVariant('Paywall Test'), 'Control');
      expect(service.getVariant('Non-existent'), isNull);
    });

    test('notifies listeners on fetch', () async {
      int notifyCount = 0;
      service.addListener(() => notifyCount++);

      await service.fetchAllocations();

      // Should notify at least twice: loading start and loading end
      expect(notifyCount, greaterThanOrEqualTo(2));
    });

    test('isLoading is false after fetch completes', () async {
      await service.fetchAllocations();

      expect(service.isLoading, false);
    });

    test('handles failure gracefully with fallback', () async {
      // First fetch succeeds
      await service.fetchAllocations();
      expect(service.allocations.length, 2);

      // Replace API client with a failing one
      service.invalidateCache();
      final failingService = ExperimentService(
        apiClient: ExperimentApiClient(
          apiBaseUrl: Uri.parse('https://api.example.com'),
          tokenProvider: () => 'test-token',
          httpClient: FailingHttpClient(),
        ),
        cacheTtl: const Duration(minutes: 5),
      );

      // Fetch with failing client should not throw and return empty list
      final result = await failingService.fetchAllocations();
      expect(result, isEmpty);
    });
  });
}
