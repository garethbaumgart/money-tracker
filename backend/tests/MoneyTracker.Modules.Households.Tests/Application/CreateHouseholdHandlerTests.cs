using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Tests.Application;

public sealed class CreateHouseholdHandlerTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleAsync_PersistsHousehold_WhenNameIsUnique()
    {
        var repository = new FakeHouseholdRepository(existsByName: false);
        var handler = new CreateHouseholdHandler(repository, new FakeTimeProvider(DateTimeOffset.Parse("2026-02-03T04:05:06Z")));

        var result = await handler.HandleAsync(new CreateHouseholdCommand("Primary Home"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Household);
        Assert.Equal("Primary Home", result.Household!.Name);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleAsync_ReturnsConflict_WhenNameAlreadyExistsCaseInsensitive()
    {
        var repository = new FakeHouseholdRepository(existsByName: true);
        var handler = new CreateHouseholdHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(new CreateHouseholdCommand("shared"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.HouseholdNameConflict, result.ErrorCode);
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleAsync_ReturnsValidationError_WhenNameInvalid()
    {
        var repository = new FakeHouseholdRepository(existsByName: false);
        var handler = new CreateHouseholdHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(new CreateHouseholdCommand("   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.ValidationError, result.ErrorCode);
        Assert.Equal(0, repository.ExistsCalls);
        Assert.Equal(0, repository.AddCalls);
    }
}

internal sealed class FakeHouseholdRepository(bool existsByName) : IHouseholdRepository
{
    public int ExistsCalls { get; private set; }
    public int AddCalls { get; private set; }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        ExistsCalls++;
        return Task.FromResult(existsByName);
    }

    public Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        AddCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
