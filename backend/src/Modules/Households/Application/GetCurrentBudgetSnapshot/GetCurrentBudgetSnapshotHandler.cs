using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Households.Application.GetCurrentBudgetSnapshot;

public sealed class GetCurrentBudgetSnapshotHandler(
    IBudgetRepository budgetRepository,
    ITransactionRepository transactionRepository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
{
    public async Task<GetCurrentBudgetSnapshotResult> HandleAsync(
        GetCurrentBudgetSnapshotQuery query,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            query.HouseholdId,
            query.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return GetCurrentBudgetSnapshotResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return GetCurrentBudgetSnapshotResult.AccessDenied();
        }

        var nowUtc = timeProvider.GetUtcNow();
        var periodStartUtc = BudgetPeriod.GetPeriodStart(nowUtc);
        var periodEndUtc = BudgetPeriod.GetPeriodEnd(periodStartUtc);
        var periodEndInclusive = periodEndUtc.AddTicks(-1);

        var categories = await budgetRepository.GetCategoriesAsync(query.HouseholdId, cancellationToken);
        var allocations = await budgetRepository.GetAllocationsAsync(query.HouseholdId, periodStartUtc, cancellationToken);
        var transactions = await transactionRepository.GetByHouseholdAsync(
            query.HouseholdId,
            periodStartUtc,
            periodEndInclusive,
            cancellationToken);

        var allocationByCategory = allocations.ToDictionary(allocation => allocation.CategoryId.Value, allocation => allocation.Amount);
        var spendByCategory = transactions
            .Where(transaction => transaction.CategoryId.HasValue)
            .GroupBy(transaction => transaction.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(transaction => transaction.Amount));

        var categorySnapshots = categories
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(category =>
            {
                allocationByCategory.TryGetValue(category.Id.Value, out var allocated);
                spendByCategory.TryGetValue(category.Id.Value, out var spent);
                var remaining = allocated - spent;
                return new BudgetCategorySnapshot(category.Id.Value, category.Name, allocated, spent, remaining);
            })
            .ToArray();

        var totalAllocated = allocationByCategory.Values.Sum();
        var totalSpent = transactions.Sum(transaction => transaction.Amount);
        var uncategorizedSpent = transactions
            .Where(transaction => transaction.CategoryId is null)
            .Sum(transaction => transaction.Amount);

        var snapshot = new CurrentBudgetSnapshot(
            query.HouseholdId,
            periodStartUtc,
            periodEndUtc,
            totalAllocated,
            totalSpent,
            totalAllocated - totalSpent,
            uncategorizedSpent,
            categorySnapshots);

        return GetCurrentBudgetSnapshotResult.Success(snapshot);
    }
}
