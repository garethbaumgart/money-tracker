namespace MoneyTracker.Modules.Budgets.Application.UpsertBudgetAllocation;

public sealed record UpsertBudgetAllocationCommand(
    Guid HouseholdId,
    Guid CategoryId,
    decimal Amount,
    DateTimeOffset? PeriodStartUtc,
    Guid RequestingUserId);
