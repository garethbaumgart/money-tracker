enum FeatureKey {
  bankSync,
  premiumInsights,
  unlimitedBudgets,
  unlimitedBillReminders,
  exportData;

  static FeatureKey? fromString(String value) {
    final normalized = value.toLowerCase();
    for (final key in FeatureKey.values) {
      if (key.name.toLowerCase() == normalized) {
        return key;
      }
    }
    // Handle PascalCase from API (e.g., "BankSync" -> bankSync)
    final camelCase = value[0].toLowerCase() + value.substring(1);
    for (final key in FeatureKey.values) {
      if (key.name == camelCase) {
        return key;
      }
    }
    return null;
  }

  String toApiString() {
    // Convert camelCase to PascalCase for API
    return name[0].toUpperCase() + name.substring(1);
  }
}
