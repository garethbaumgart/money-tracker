namespace MoneyTracker.Modules.Budgets.Application.CreateBudgetCategory;

public sealed record CreateBudgetCategoryCommand(
    Guid HouseholdId,
    string Name,
    Guid RequestingUserId);
