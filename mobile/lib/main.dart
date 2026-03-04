import 'package:flutter/material.dart';

void main() {
  runApp(const MoneyTrackerApp());
}

class MoneyTrackerApp extends StatelessWidget {
  const MoneyTrackerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Money Tracker',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF1F7A8C)),
        useMaterial3: true,
      ),
      home: const MoneyTrackerHomePage(),
    );
  }
}

class MoneyTrackerHomePage extends StatelessWidget {
  const MoneyTrackerHomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Money Tracker')),
      body: const Center(
        child: Text('Mobile shell is ready for Phase 1 feature slices.'),
      ),
    );
  }
}
