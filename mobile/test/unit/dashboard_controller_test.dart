import 'package:flutter_test/flutter_test.dart';
import 'package:money_tracker/features/dashboard/dashboard_controller.dart';

void main() {
  test('refresh increments refresh count', () async {
    final controller = DashboardController(
      initialState: DashboardState.empty(),
    );
    addTearDown(controller.dispose);

    final before = controller.state.refreshCount;
    await controller.refresh();

    expect(controller.state.refreshCount, before + 1);
  });
}
