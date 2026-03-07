using MoneyTracker.Modules.Auth.Application.DeleteUser;
using MoneyTracker.Modules.Auth.Infrastructure;

namespace MoneyTracker.Modules.Auth.Tests;

public sealed class DeleteUserHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_MarksUserDeletedWithPurgeSchedule()
    {
        var repository = new InMemoryAuthRepository();
        var user = await repository.GetOrCreateUserAsync("test@example.com", CancellationToken.None);
        var handler = new DeleteUserHandler(repository, new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new DeleteUserCommand(user.Id),
            [],
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ScheduledPurgeAtUtc);
        Assert.Equal(NowUtc.AddDays(30), result.ScheduledPurgeAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_CallsAllDeletionParticipants()
    {
        var repository = new InMemoryAuthRepository();
        var user = await repository.GetOrCreateUserAsync("test@example.com", CancellationToken.None);
        var handler = new DeleteUserHandler(repository, new StubTimeProvider(NowUtc));

        var calledUserIds = new List<Guid>();
        var participants = new List<Func<Guid, CancellationToken, Task>>
        {
            (userId, ct) => { calledUserIds.Add(userId); return Task.CompletedTask; },
            (userId, ct) => { calledUserIds.Add(userId); return Task.CompletedTask; },
        };

        var result = await handler.HandleAsync(
            new DeleteUserCommand(user.Id),
            participants,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, calledUserIds.Count);
        Assert.All(calledUserIds, id => Assert.Equal(user.Id, id));
    }
}

internal sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
