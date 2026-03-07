import 'package:money_tracker/app/bootstrap/app_bootstrap.dart';
import 'package:money_tracker/app/observability/crash_reporter.dart';
import 'package:money_tracker/app/observability/performance_monitor.dart';
import 'package:money_tracker/app/observability/startup_error_reporter.dart';

Future<void> main() async {
  const errorReporter = FlutterErrorStartupErrorReporter();
  const crashReporter = ConsoleCrashReporter();
  final performanceMonitor = PerformanceMonitor();

  await runMoneyTrackerApp(
    errorReporter: errorReporter,
    crashReporter: crashReporter,
    performanceMonitor: performanceMonitor,
  );
}
