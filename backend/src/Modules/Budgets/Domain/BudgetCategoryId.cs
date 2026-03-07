namespace MoneyTracker.Modules.Budgets.Domain;

public readonly record struct BudgetCategoryId(Guid Value)
{
    public static BudgetCategoryId New() => new(Guid.NewGuid());
}
