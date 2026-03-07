using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Households.Tests.Application;

public sealed class GetHouseholdDashboardHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsEmptyDashboard_WhenNoData()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.Parse("2026-03-05T06:00:00Z");
        var handler = new GetHouseholdDashboardHandler(
            new EmptyBudgetRepository(),
            new EmptyTransactionRepository(),
            new FakeHouseholdAccessService(HouseholdAccessResult.Allowed()),
            new FixedTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new GetHouseholdDashboardQuery(householdId, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Dashboard);
        var dashboard = result.Dashboard!;
        Assert.Equal(householdId, dashboard.HouseholdId);
        Assert.Equal(BudgetPeriod.GetPeriodStart(nowUtc), dashboard.PeriodStartUtc);
        Assert.Equal(BudgetPeriod.GetPeriodEnd(dashboard.PeriodStartUtc), dashboard.PeriodEndUtc);
        Assert.Empty(dashboard.Categories);
        Assert.Empty(dashboard.RecentTransactions);
        Assert.Equal(0m, dashboard.TotalAllocated);
        Assert.Equal(0m, dashboard.TotalSpent);
        Assert.Equal(0m, dashboard.TotalRemaining);
        Assert.Equal(0m, dashboard.UncategorizedSpent);
    }
}

internal sealed class EmptyBudgetRepository : IBudgetRepository
{
    public Task<bool> AddCategoryAsync(BudgetCategory category, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<BudgetCategory?> GetCategoryAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<BudgetCategory?>(null);
    }

    public Task<BudgetCategory?> GetCategoryByNameAsync(
        Guid householdId,
        string normalizedName,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<BudgetCategory?>(null);
    }

    public Task<IReadOnlyCollection<BudgetCategory>> GetCategoriesAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<BudgetCategory>>(Array.Empty<BudgetCategory>());
    }

    public Task<BudgetAllocation?> GetAllocationAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<BudgetAllocation?>(null);
    }

    public Task<IReadOnlyCollection<BudgetAllocation>> GetAllocationsAsync(
        Guid householdId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<BudgetAllocation>>(Array.Empty<BudgetAllocation>());
    }

    public Task<BudgetAllocation> UpsertAllocationAsync(
        BudgetAllocation allocation,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(allocation);
    }
}

internal sealed class EmptyTransactionRepository : ITransactionRepository
{
    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Transaction>> GetByHouseholdAsync(
        Guid householdId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Transaction>>(Array.Empty<Transaction>());
    }
}

internal sealed class FakeHouseholdAccessService(HouseholdAccessResult accessResult) : IHouseholdAccessService
{
    public Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(accessResult);
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
