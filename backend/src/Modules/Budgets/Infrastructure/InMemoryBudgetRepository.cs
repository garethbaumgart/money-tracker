using MoneyTracker.Modules.Budgets.Domain;

namespace MoneyTracker.Modules.Budgets.Infrastructure;

// PostgreSQL index recommendations:
// - IX_budget_categories_household_name: UNIQUE index on budget_categories(household_id, LOWER(normalized_name))
// - IX_budget_categories_household_id: index on budget_categories(household_id, id)
// - IX_budget_allocations_lookup: UNIQUE index on budget_allocations(household_id, category_id, period_start_utc)
// - IX_budget_allocations_period: index on budget_allocations(household_id, period_start_utc) for period queries
public sealed class InMemoryBudgetRepository : IBudgetRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Dictionary<string, BudgetCategory>> _categoriesByHouseholdName =
        new();
    private readonly Dictionary<Guid, Dictionary<BudgetCategoryId, BudgetCategory>> _categoriesByHouseholdId =
        new();
    private readonly Dictionary<Guid, Dictionary<(BudgetCategoryId, DateTimeOffset), BudgetAllocation>>
        _allocationsByHousehold = new();

    public Task<bool> AddCategoryAsync(BudgetCategory category, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<bool>(cancellationToken);
        }

        lock (_sync)
        {
            var nameLookup = GetCategoryNameLookup(category.HouseholdId);
            if (nameLookup.ContainsKey(category.NormalizedName))
            {
                return Task.FromResult(false);
            }

            nameLookup[category.NormalizedName] = category;
            var idLookup = GetCategoryIdLookup(category.HouseholdId);
            idLookup[category.Id] = category;
            return Task.FromResult(true);
        }
    }

    public Task<BudgetCategory?> GetCategoryAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<BudgetCategory?>(cancellationToken);
        }

        lock (_sync)
        {
            var idLookup = GetCategoryIdLookup(householdId);
            return Task.FromResult<BudgetCategory?>(idLookup.GetValueOrDefault(categoryId));
        }
    }

    public Task<BudgetCategory?> GetCategoryByNameAsync(
        Guid householdId,
        string normalizedName,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<BudgetCategory?>(cancellationToken);
        }

        lock (_sync)
        {
            var nameLookup = GetCategoryNameLookup(householdId);
            return Task.FromResult<BudgetCategory?>(nameLookup.GetValueOrDefault(normalizedName));
        }
    }

    public Task<IReadOnlyCollection<BudgetCategory>> GetCategoriesAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BudgetCategory>>(cancellationToken);
        }

        lock (_sync)
        {
            var idLookup = GetCategoryIdLookup(householdId);
            return Task.FromResult<IReadOnlyCollection<BudgetCategory>>(idLookup.Values.ToArray());
        }
    }

    public Task<BudgetAllocation?> GetAllocationAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<BudgetAllocation?>(cancellationToken);
        }

        lock (_sync)
        {
            var allocations = GetAllocationLookup(householdId);
            return Task.FromResult<BudgetAllocation?>(allocations.GetValueOrDefault((categoryId, periodStartUtc)));
        }
    }

    public Task<IReadOnlyCollection<BudgetAllocation>> GetAllocationsAsync(
        Guid householdId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IReadOnlyCollection<BudgetAllocation>>(cancellationToken);
        }

        lock (_sync)
        {
            var allocations = GetAllocationLookup(householdId);
            var results = allocations.Values
                .Where(allocation => allocation.PeriodStartUtc == periodStartUtc)
                .ToArray();
            return Task.FromResult<IReadOnlyCollection<BudgetAllocation>>(results);
        }
    }

    public Task<BudgetAllocation> UpsertAllocationAsync(
        BudgetAllocation allocation,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<BudgetAllocation>(cancellationToken);
        }

        lock (_sync)
        {
            var allocations = GetAllocationLookup(allocation.HouseholdId);
            allocations[(allocation.CategoryId, allocation.PeriodStartUtc)] = allocation;
            return Task.FromResult(allocation);
        }
    }

    private Dictionary<string, BudgetCategory> GetCategoryNameLookup(Guid householdId)
    {
        if (!_categoriesByHouseholdName.TryGetValue(householdId, out var lookup))
        {
            lookup = new Dictionary<string, BudgetCategory>(StringComparer.OrdinalIgnoreCase);
            _categoriesByHouseholdName[householdId] = lookup;
        }

        return lookup;
    }

    private Dictionary<BudgetCategoryId, BudgetCategory> GetCategoryIdLookup(Guid householdId)
    {
        if (!_categoriesByHouseholdId.TryGetValue(householdId, out var lookup))
        {
            lookup = new Dictionary<BudgetCategoryId, BudgetCategory>();
            _categoriesByHouseholdId[householdId] = lookup;
        }

        return lookup;
    }

    private Dictionary<(BudgetCategoryId, DateTimeOffset), BudgetAllocation> GetAllocationLookup(Guid householdId)
    {
        if (!_allocationsByHousehold.TryGetValue(householdId, out var lookup))
        {
            lookup = new Dictionary<(BudgetCategoryId, DateTimeOffset), BudgetAllocation>();
            _allocationsByHousehold[householdId] = lookup;
        }

        return lookup;
    }
}
