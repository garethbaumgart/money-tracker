using MoneyTracker.Modules.BillReminders.Application.CreateBillReminder;
using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.BillReminders.Infrastructure;
using MoneyTracker.Modules.Households.Domain;

namespace MoneyTracker.Modules.BillReminders.Tests.Application;

public sealed class CreateBillReminderHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsValidationError_WhenAmountIsInvalid()
    {
        var ownerId = Guid.NewGuid();
        var household = Household.Create("Payments", ownerId, DateTimeOffset.UtcNow);
        var handler = new CreateBillReminderHandler(
            new InMemoryBillReminderRepository(),
            new FakeHouseholdRepository(household),
            TimeProvider.System);

        var result = await handler.HandleAsync(
            new CreateBillReminderCommand(
                household.Id.Value,
                "Rent",
                0,
                DateTimeOffset.UtcNow.AddDays(3),
                BillReminderCadence.Monthly,
                ownerId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BillReminderErrors.ReminderAmountInvalid, result.ErrorCode);
    }
}

internal sealed class FakeHouseholdRepository(Household? household) : IHouseholdRepository
{
    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<Household?> GetByIdAsync(HouseholdId householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult(household);
    }

    public Task<bool> IsMemberAsync(HouseholdId householdId, Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
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
