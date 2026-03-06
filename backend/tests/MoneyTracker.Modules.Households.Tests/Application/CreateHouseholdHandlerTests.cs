using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Tests.Application;

public sealed class CreateHouseholdHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_PersistsHousehold_WhenNameIsUnique()
    {
        var repository = new FakeHouseholdRepository(addSucceeds: true);
        var expectedCreatedAtUtc = DateTimeOffset.Parse("2026-02-03T04:05:06Z");
        var handler = new CreateHouseholdHandler(repository, new FakeTimeProvider(expectedCreatedAtUtc));

        var result = await handler.HandleAsync(new CreateHouseholdCommand("Primary Home"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Household);
        Assert.Equal("Primary Home", result.Household!.Name);
        Assert.Equal(expectedCreatedAtUtc, result.Household.CreatedAtUtc);
        Assert.Equal(1, repository.AddIfNotExistsCalls);
        Assert.Equal(expectedCreatedAtUtc, repository.LastAddedHousehold?.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsConflict_WhenNameAlreadyExistsCaseInsensitive()
    {
        var repository = new FakeHouseholdRepository(addSucceeds: false);
        var handler = new CreateHouseholdHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(new CreateHouseholdCommand("shared"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.HouseholdNameConflict, result.ErrorCode);
        Assert.Equal(1, repository.AddIfNotExistsCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsValidationError_WhenNameInvalid()
    {
        var repository = new FakeHouseholdRepository(addSucceeds: true);
        var handler = new CreateHouseholdHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(new CreateHouseholdCommand("   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.ValidationError, result.ErrorCode);
        Assert.Equal(0, repository.AddIfNotExistsCalls);
    }
}

internal sealed class FakeHouseholdRepository(bool addSucceeds) : IHouseholdRepository
{
    public int AddIfNotExistsCalls { get; private set; }
    public Household? LastAddedHousehold { get; private set; }

    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        AddIfNotExistsCalls++;
        if (addSucceeds)
        {
            LastAddedHousehold = household;
        }

        return Task.FromResult(addSucceeds);
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
