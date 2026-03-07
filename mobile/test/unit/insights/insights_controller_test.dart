import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/insights/application/insights_controller.dart';

void main() {
  group('InsightsController', () {
    test('initial state has idle load state', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      expect(controller.state.loadState, InsightsLoadState.idle);
      expect(controller.state.selectedPeriod, '30d');
      expect(controller.state.spendingAnalysis, isNull);
      expect(controller.state.budgetHealth, isNull);
    });

    test('selectPeriod updates selected period and notifies', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      var notifyCount = 0;
      controller.addListener(() => notifyCount++);

      controller.selectPeriod('7d');

      expect(controller.state.selectedPeriod, '7d');
      expect(notifyCount, 1);
    });

    test('selectPeriod with same period does not notify', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      var notifyCount = 0;
      controller.addListener(() => notifyCount++);

      controller.selectPeriod('30d'); // same as default

      expect(notifyCount, 0);
    });

    test('seedSample populates state with sample data', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      controller.seedSample();

      expect(controller.state.loadState, InsightsLoadState.loaded);
      expect(controller.state.spendingAnalysis, isNotNull);
      expect(controller.state.budgetHealth, isNotNull);
      expect(
          controller.state.spendingAnalysis!.categories.length, greaterThan(0));
      expect(
          controller.state.budgetHealth!.categoryHealth.length, greaterThan(0));
    });

    test('seedSample is idempotent', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      var notifyCount = 0;
      controller.addListener(() => notifyCount++);

      controller.seedSample();
      controller.seedSample();

      expect(notifyCount, 1);
    });

    test('sample data has anomalies', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      controller.seedSample();

      expect(
          controller.state.spendingAnalysis!.anomalies.length, greaterThan(0));
    });

    test('sample data budget health has overall score in valid range', () {
      final controller = InsightsController();
      addTearDown(controller.dispose);

      controller.seedSample();

      final score = controller.state.budgetHealth!.overallScore;
      expect(score, greaterThanOrEqualTo(0));
      expect(score, lessThanOrEqualTo(100));
    });
  });

  group('InsightsState', () {
    test('isLoading is true only when loading', () {
      final state =
          InsightsState(loadState: InsightsLoadState.loading);
      expect(state.isLoading, isTrue);
      expect(state.hasData, isFalse);
    });

    test('isPremiumRequired is true only for premiumRequired', () {
      final state =
          InsightsState(loadState: InsightsLoadState.premiumRequired);
      expect(state.isPremiumRequired, isTrue);
      expect(state.hasData, isFalse);
    });

    test('hasData is true only when loaded', () {
      final state =
          InsightsState(loadState: InsightsLoadState.loaded);
      expect(state.hasData, isTrue);
    });

    test('hasError is true only when error', () {
      final state = InsightsState(
        loadState: InsightsLoadState.error,
        errorMessage: 'Test error',
      );
      expect(state.hasError, isTrue);
      expect(state.errorMessage, 'Test error');
    });
  });
}
