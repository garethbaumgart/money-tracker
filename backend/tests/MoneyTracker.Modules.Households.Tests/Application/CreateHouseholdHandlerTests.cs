using MoneyTracker.Modules.Households.Application.CreateHousehold;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.Households.Tests.Application;

public sealed class CreateHouseholdHandlerTests
{
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_PersistsHousehold_WhenNameIsUnique()
    {
        var repository = new FakeHouseholdRepository(addSucceeds: true);
        var expectedCreatedAtUtc = DateTimeOffset.Parse("2026-02-03T04:05:06Z");
        var handler = new CreateHouseholdHandler(repository, new FakeTimeProvider(expectedCreatedAtUtc));

        var result = await handler.HandleAsync(new CreateHouseholdCommand("Primary Home", OwnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Household);
        Assert.Equal("Primary Home", result.Household!.Name);
        Assert.Equal(expectedCreatedAtUtc, result.Household.CreatedAtUtc);
        Assert.Equal(OwnerUserId, result.Household.OwnerUserId);
        Assert.Equal(OwnerUserId, result.Household.Members.Single().UserId);
        Assert.Equal(HouseholdRole.Owner, result.Household.Members.Single().Role);
        Assert.Equal(1, repository.AddIfNotExistsCalls);
        Assert.Equal(expectedCreatedAtUtc, repository.LastAddedHousehold?.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsConflict_WhenNameAlreadyExistsCaseInsensitive()
    {
        var repository = new FakeHouseholdRepository(addSucceeds: true, existingName: " Shared ");
        var handler = new CreateHouseholdHandler(repository, TimeProvider.System);

        var result = await handler.HandleAsync(new CreateHouseholdCommand("sHaReD", OwnerUserId), CancellationToken.None);

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

        var result = await handler.HandleAsync(new CreateHouseholdCommand("   ", OwnerUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HouseholdErrors.ValidationError, result.ErrorCode);
        Assert.Equal(0, repository.AddIfNotExistsCalls);
    }
}

internal sealed class FakeHouseholdRepository(bool addSucceeds, string? existingName = null) : IHouseholdRepository
{
    private readonly HashSet<string> _existingNames = new(
        new[] { Household.NormalizeName(existingName) },
        StringComparer.OrdinalIgnoreCase);

    public int AddIfNotExistsCalls { get; private set; }
    public Household? LastAddedHousehold { get; private set; }

    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        AddIfNotExistsCalls++;
        if (_existingNames.Contains(household.Name))
        {
            return Task.FromResult(false);
        }

        _existingNames.Add(household.Name);

        if (addSucceeds)
        {
            LastAddedHousehold = household;
        }

        return Task.FromResult(addSucceeds);
    }

    public Task<Household?> GetByIdAsync(HouseholdId householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<Household?>(null);
    }

    public Task<bool> IsMemberAsync(HouseholdId householdId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> AddMemberAsync(HouseholdId householdId, Guid userId, string role, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> AddInvitationAsync(HouseholdInvitation invitation, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<HouseholdInvitation?> GetInvitationAsync(string invitationToken, CancellationToken cancellationToken)
    {
        return Task.FromResult<HouseholdInvitation?>(null);
    }

    public Task<bool> MarkInvitationUsedAsync(
        string invitationToken,
        Guid acceptingUserId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
