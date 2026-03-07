namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record CohortRetention(
    string Week,
    int Signups,
    double? D1,
    double? D7,
    double? D14,
    double? D30);
