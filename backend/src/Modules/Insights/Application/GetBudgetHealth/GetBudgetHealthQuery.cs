namespace MoneyTracker.Modules.Insights.Application.GetBudgetHealth;

public sealed record GetBudgetHealthQuery(
    Guid HouseholdId,
    Guid UserId);
