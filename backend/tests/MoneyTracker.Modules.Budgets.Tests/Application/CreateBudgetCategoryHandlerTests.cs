using MoneyTracker.Modules.Budgets.Application.CreateBudgetCategory;
using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.Budgets.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.Budgets.Tests.Application;

public sealed class CreateBudgetCategoryHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsConflict_WhenNameAlreadyExistsCaseInsensitive()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new InMemoryBudgetRepository();
        var handler = new CreateBudgetCategoryHandler(
            repository,
            new AllowedHouseholdAccessService(),
            TimeProvider.System);

        var first = await handler.HandleAsync(
            new CreateBudgetCategoryCommand(householdId, "Dining", userId),
            CancellationToken.None);
        var result = await handler.HandleAsync(
            new CreateBudgetCategoryCommand(householdId, "DINING", userId),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.False(result.IsSuccess);
        Assert.Equal(BudgetErrors.BudgetCategoryNameConflict, result.ErrorCode);
    }
}

internal sealed class AllowedHouseholdAccessService : IHouseholdAccessService
{
    public Task<HouseholdAccessResult> CheckMemberAsync(
        Guid householdId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(HouseholdAccessResult.Allowed());
    }

    public Task<IReadOnlyCollection<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
    }
}
