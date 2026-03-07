using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;

public sealed class GetHouseholdDashboardHandler(
    IBudgetRepository budgetRepository,
    ITransactionRepository transactionRepository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
{
    private const int RecentTransactionLimit = 5;

    public async Task<GetHouseholdDashboardResult> HandleAsync(
        GetHouseholdDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            query.HouseholdId,
            query.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return GetHouseholdDashboardResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return GetHouseholdDashboardResult.AccessDenied();
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

        var allocationByCategory = allocations.ToDictionary(
            allocation => allocation.CategoryId.Value,
            allocation => allocation.Amount);
        var spendByCategory = transactions
            .Where(transaction => transaction.CategoryId.HasValue)
            .GroupBy(transaction => transaction.CategoryId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(transaction => transaction.Amount));

        var categorySummaries = categories
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .Select(category =>
            {
                allocationByCategory.TryGetValue(category.Id.Value, out var allocated);
                spendByCategory.TryGetValue(category.Id.Value, out var spent);
                var remaining = allocated - spent;
                return new DashboardCategorySummary(category.Id.Value, category.Name, allocated, spent, remaining);
            })
            .ToArray();

        var totalAllocated = allocationByCategory.Values.Sum();
        var totalSpent = transactions.Sum(transaction => transaction.Amount);
        var uncategorizedSpent = transactions
            .Where(transaction => transaction.CategoryId is null)
            .Sum(transaction => transaction.Amount);

        var categoryLookup = categories.ToDictionary(category => category.Id.Value, category => category.Name);
        var recentTransactions = transactions
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .Take(RecentTransactionLimit)
            .Select(transaction => new DashboardTransactionSummary(
                transaction.Id.Value,
                transaction.Amount,
                transaction.OccurredAtUtc,
                transaction.Description,
                transaction.CategoryId,
                transaction.CategoryId.HasValue && categoryLookup.TryGetValue(transaction.CategoryId.Value, out var name)
                    ? name
                    : null))
            .ToArray();

        var dashboard = new HouseholdDashboard(
            query.HouseholdId,
            periodStartUtc,
            periodEndUtc,
            totalAllocated,
            totalSpent,
            totalAllocated - totalSpent,
            uncategorizedSpent,
            categorySummaries,
            recentTransactions);

        return GetHouseholdDashboardResult.Success(dashboard);
    }
}
