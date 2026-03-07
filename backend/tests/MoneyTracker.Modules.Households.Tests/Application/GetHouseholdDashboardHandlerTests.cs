using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;
using MoneyTracker.Modules.Households.Domain;
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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsNotFound_WhenHouseholdMissing()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.Parse("2026-03-05T06:00:00Z");
        var handler = new GetHouseholdDashboardHandler(
            new EmptyBudgetRepository(),
            new EmptyTransactionRepository(),
            new FakeHouseholdAccessService(HouseholdAccessResult.NotFound()),
            new FixedTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new GetHouseholdDashboardQuery(householdId, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.HouseholdNotFound, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsAccessDenied_WhenUserNotMember()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.Parse("2026-03-05T06:00:00Z");
        var handler = new GetHouseholdDashboardHandler(
            new EmptyBudgetRepository(),
            new EmptyTransactionRepository(),
            new FakeHouseholdAccessService(HouseholdAccessResult.Denied()),
            new FixedTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new GetHouseholdDashboardQuery(householdId, userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.HouseholdAccessDenied, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsDashboard_WithBudgetAndTransactions()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.Parse("2026-03-05T06:00:00Z");
        var periodStart = BudgetPeriod.GetPeriodStart(nowUtc);

        var category = BudgetCategory.Create(householdId, "Groceries", userId, nowUtc);
        var allocation = BudgetAllocation.Create(
            householdId,
            category.Id,
            500m,
            periodStart,
            userId,
            nowUtc);

        var transactions = new[]
        {
            Transaction.Create(
                householdId,
                userId,
                120m,
                nowUtc.AddDays(-1),
                "Market",
                category.Id.Value,
                nowUtc),
            Transaction.Create(
                householdId,
                userId,
                30m,
                nowUtc.AddDays(-2),
                "Cash",
                null,
                nowUtc)
        };

        var handler = new GetHouseholdDashboardHandler(
            new FakeBudgetRepository(new[] { category }, new[] { allocation }),
            new FakeTransactionRepository(transactions),
            new FakeHouseholdAccessService(HouseholdAccessResult.Allowed()),
            new FixedTimeProvider(nowUtc));

        var result = await handler.HandleAsync(
            new GetHouseholdDashboardQuery(householdId, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Dashboard);
        var dashboard = result.Dashboard!;
        Assert.Equal(500m, dashboard.TotalAllocated);
        Assert.Equal(150m, dashboard.TotalSpent);
        Assert.Equal(350m, dashboard.TotalRemaining);
        Assert.Equal(30m, dashboard.UncategorizedSpent);
        Assert.Single(dashboard.Categories);

        var categorySummary = dashboard.Categories[0];
        Assert.Equal(category.Id.Value, categorySummary.CategoryId);
        Assert.Equal("Groceries", categorySummary.Name);
        Assert.Equal(500m, categorySummary.Allocated);
        Assert.Equal(120m, categorySummary.Spent);
        Assert.Equal(380m, categorySummary.Remaining);

        Assert.Equal(2, dashboard.RecentTransactions.Length);
        Assert.Equal(transactions[0].Id.Value, dashboard.RecentTransactions[0].Id);
        Assert.Equal("Groceries", dashboard.RecentTransactions[0].CategoryName);
        Assert.Null(dashboard.RecentTransactions[1].CategoryName);
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

internal sealed class FakeBudgetRepository(
    IReadOnlyCollection<BudgetCategory> categories,
    IReadOnlyCollection<BudgetAllocation> allocations) : IBudgetRepository
{
    private readonly IReadOnlyCollection<BudgetCategory> _categories = categories;
    private readonly IReadOnlyCollection<BudgetAllocation> _allocations = allocations;

    public Task<bool> AddCategoryAsync(BudgetCategory category, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<BudgetCategory?> GetCategoryAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        CancellationToken cancellationToken)
    {
        foreach (var category in _categories)
        {
            if (category.Id == categoryId)
            {
                return Task.FromResult<BudgetCategory?>(category);
            }
        }

        return Task.FromResult<BudgetCategory?>(null);
    }

    public Task<BudgetCategory?> GetCategoryByNameAsync(
        Guid householdId,
        string normalizedName,
        CancellationToken cancellationToken)
    {
        foreach (var category in _categories)
        {
            if (category.NormalizedName == normalizedName)
            {
                return Task.FromResult<BudgetCategory?>(category);
            }
        }

        return Task.FromResult<BudgetCategory?>(null);
    }

    public Task<IReadOnlyCollection<BudgetCategory>> GetCategoriesAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories);
    }

    public Task<BudgetAllocation?> GetAllocationAsync(
        Guid householdId,
        BudgetCategoryId categoryId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        foreach (var allocation in _allocations)
        {
            if (allocation.CategoryId == categoryId && allocation.PeriodStartUtc == periodStartUtc)
            {
                return Task.FromResult<BudgetAllocation?>(allocation);
            }
        }

        return Task.FromResult<BudgetAllocation?>(null);
    }

    public Task<IReadOnlyCollection<BudgetAllocation>> GetAllocationsAsync(
        Guid householdId,
        DateTimeOffset periodStartUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_allocations);
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

    public Task AddRangeAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken)
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

    public Task<bool> ExistsByExternalIdAsync(Guid bankConnectionId, string externalTransactionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}

internal sealed class FakeTransactionRepository(IReadOnlyCollection<Transaction> transactions) : ITransactionRepository
{
    private readonly IReadOnlyCollection<Transaction> _transactions = transactions;

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Transaction>> GetByHouseholdAsync(
        Guid householdId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_transactions);
    }

    public Task<bool> ExistsByExternalIdAsync(Guid bankConnectionId, string externalTransactionId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
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
