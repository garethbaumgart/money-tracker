namespace MoneyTracker.Modules.Budgets.Domain;

public interface IBudgetRepository
{
    Task<bool> AddCategoryAsync(BudgetCategory category, CancellationToken cancellationToken);
    Task<BudgetCategory?> GetCategoryAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        CancellationToken cancellationToken);
    Task<BudgetCategory?> GetCategoryByNameAsync(
        Guid householdId,
        string normalizedName,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BudgetCategory>> GetCategoriesAsync(
        Guid householdId,
        CancellationToken cancellationToken);

    Task<BudgetAllocation?> GetAllocationAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<BudgetAllocation>> GetAllocationsAsync(
        Guid householdId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken);
    Task<BudgetAllocation> UpsertAllocationAsync(
        BudgetAllocation allocation,
        CancellationToken cancellationToken);
}
