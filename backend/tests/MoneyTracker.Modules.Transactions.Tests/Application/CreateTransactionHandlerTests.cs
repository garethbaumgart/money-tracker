using MoneyTracker.Modules.Budgets.Infrastructure;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.Transactions.Application.CreateTransaction;
using MoneyTracker.Modules.Transactions.Domain;
using MoneyTracker.Modules.Transactions.Infrastructure;

namespace MoneyTracker.Modules.Transactions.Tests.Application;

public sealed class CreateTransactionHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsValidationError_WhenAmountIsZero()
    {
        var householdId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");
        var handler = new CreateTransactionHandler(
            new InMemoryTransactionRepository(),
            new InMemoryBudgetRepository(),
            new AllowedHouseholdAccessService(),
            new FakeTimeProvider(nowUtc),
            new NoopAnalyticsEventPublisher());

        var result = await handler.HandleAsync(
            new CreateTransactionCommand(
                householdId,
                0,
                nowUtc,
                "Rent",
                null,
                userId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(TransactionErrors.TransactionAmountInvalid, result.ErrorCode);
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

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
