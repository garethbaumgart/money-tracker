namespace MoneyTracker.Modules.Budgets.Domain;

public readonly record struct BudgetAllocationId(Guid Value)
{
    public static BudgetAllocationId New() => new(Guid.NewGuid());
}
