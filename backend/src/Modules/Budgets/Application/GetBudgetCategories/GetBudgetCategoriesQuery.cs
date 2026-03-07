namespace MoneyTracker.Modules.Budgets.Application.GetBudgetCategories;

public sealed record GetBudgetCategoriesQuery(Guid HouseholdId, Guid RequestingUserId);
