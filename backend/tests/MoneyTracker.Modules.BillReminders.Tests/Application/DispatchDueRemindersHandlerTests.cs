using MoneyTracker.Modules.BillReminders.Application.DispatchDueReminders;
using MoneyTracker.Modules.BillReminders.Domain;
using MoneyTracker.Modules.BillReminders.Infrastructure;
using MoneyTracker.Modules.Households.Domain;
using MoneyTracker.Modules.Notifications.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace MoneyTracker.Modules.BillReminders.Tests.Application;

public sealed class DispatchDueRemindersHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_SkipsDispatch_WhenReminderAlreadyDispatched()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var household = Household.Create("Bills", Guid.NewGuid(), nowUtc);
        var reminder = BillReminder.Create(
            household.Id.Value,
            household.OwnerUserId,
            "Utilities",
            120m,
            nowUtc.AddDays(-1),
            BillReminderCadence.Monthly,
            nowUtc);

        var reminderRepository = new InMemoryBillReminderRepository();
        await reminderRepository.AddAsync(reminder, CancellationToken.None);
        await reminderRepository.RecordDispatchAsync(
            new BillReminderDispatchRecord(
                reminder.Id,
                reminder.NextDueDateUtc,
                nowUtc,
                true,
                ErrorCode: null,
                ErrorMessage: null),
            CancellationToken.None);

        var sender = new FakeNotificationSender();
        var handler = new DispatchDueRemindersHandler(
            reminderRepository,
            new FakeDispatchHouseholdRepository(household),
            new FakeNotificationTokenRepository(),
            sender,
            new FakeTimeProvider(nowUtc),
            NullLogger<DispatchDueRemindersHandler>.Instance);

        await handler.HandleAsync(CancellationToken.None);

        Assert.Equal(0, sender.SendCount);
    }
}

internal sealed class FakeNotificationSender : INotificationSender
{
    public int SendCount { get; private set; }

    public Task<NotificationDispatchResult> SendReminderAsync(
        NotificationMessage message,
        DeviceToken token,
        CancellationToken cancellationToken)
    {
        SendCount += 1;
        return Task.FromResult(NotificationDispatchResult.Success());
    }
}

internal sealed class FakeNotificationTokenRepository : INotificationTokenRepository
{
    public Task<DeviceToken> UpsertAsync(DeviceToken token, CancellationToken cancellationToken)
    {
        return Task.FromResult(token);
    }

    public Task<IReadOnlyCollection<DeviceToken>> GetTokensForUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<DeviceToken>>(Array.Empty<DeviceToken>());
    }
}

internal sealed class FakeDispatchHouseholdRepository(Household household) : IHouseholdRepository
{
    public Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<Household?> GetByIdAsync(HouseholdId householdId, CancellationToken cancellationToken)
    {
        return Task.FromResult<Household?>(household);
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

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
