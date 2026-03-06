import 'package:money_tracker/app/bootstrap/app_bootstrap.dart';
import 'package:money_tracker/app/observability/startup_error_reporter.dart';

Future<void> main() async {
  const errorReporter = FlutterErrorStartupErrorReporter();
  await runMoneyTrackerApp(errorReporter: errorReporter);
}
