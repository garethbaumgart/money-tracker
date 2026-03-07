namespace MoneyTracker.Modules.Analytics.Domain;

public sealed record CohortSummary(
    string CohortKey,
    int SignupCount,
    double PaidConversionRate);
