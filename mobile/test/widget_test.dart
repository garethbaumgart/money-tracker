import 'package:flutter_test/flutter_test.dart';

import 'package:money_tracker/main.dart';

void main() {
  testWidgets('Shows Money Tracker shell title', (WidgetTester tester) async {
    await tester.pumpWidget(const MoneyTrackerApp());

    expect(find.text('Money Tracker'), findsOneWidget);
    expect(
      find.text('Mobile shell is ready for Phase 1 feature slices.'),
      findsOneWidget,
    );
  });
}
