using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Application.GetSpendingSummary;

public sealed record GetSpendingSummaryQuery(
    Guid HouseholdId,
    Guid UserId,
    InsightsPeriod Period);
