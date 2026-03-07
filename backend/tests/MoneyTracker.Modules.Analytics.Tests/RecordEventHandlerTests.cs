using MoneyTracker.Modules.Analytics.Application.RecordEvent;
using MoneyTracker.Modules.Analytics.Domain;
using MoneyTracker.Modules.Analytics.Infrastructure;

namespace MoneyTracker.Modules.Analytics.Tests;

public sealed class RecordEventHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_PersistsValidBatch()
    {
        // P5-2-UNIT-01: RecordEventHandler persists valid batch of events
        var repository = new InMemoryActivationEventRepository();
        var handler = new RecordEventHandler(repository, new StubTimeProvider(NowUtc));
        var userId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("signup_completed", null, null, NowUtc),
                new RecordEventItem("household_created", Guid.NewGuid(), null, NowUtc),
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.AcceptedCount);
        Assert.Equal(0, result.DuplicateCount);

        var all = await repository.GetAllAsync(CancellationToken.None);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_RejectsInvalidMilestone()
    {
        // P5-2-UNIT-02: RecordEventHandler rejects invalid milestone name
        var repository = new InMemoryActivationEventRepository();
        var handler = new RecordEventHandler(repository, new StubTimeProvider(NowUtc));
        var userId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("not_a_real_milestone", null, null, NowUtc),
            ]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AnalyticsErrors.InvalidMilestone, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_DeduplicatesByUserIdAndMilestone()
    {
        // P5-2-UNIT-03: RecordEventHandler deduplicates by (userId, milestone)
        var repository = new InMemoryActivationEventRepository();
        var handler = new RecordEventHandler(repository, new StubTimeProvider(NowUtc));
        var userId = Guid.NewGuid();

        // First submission
        var result1 = await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("signup_completed", null, null, NowUtc),
            ]),
            CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.Equal(1, result1.AcceptedCount);
        Assert.Equal(0, result1.DuplicateCount);

        // Second submission of same milestone for same user
        var result2 = await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("signup_completed", null, null, NowUtc),
            ]),
            CancellationToken.None);

        Assert.True(result2.IsSuccess);
        Assert.Equal(0, result2.AcceptedCount);
        Assert.Equal(1, result2.DuplicateCount);

        var all = await repository.GetAllAsync(CancellationToken.None);
        Assert.Single(all);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_EmptyBatch_ReturnsValidationError()
    {
        var repository = new InMemoryActivationEventRepository();
        var handler = new RecordEventHandler(repository, new StubTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new RecordEventCommand(Guid.NewGuid(), "ios", []),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AnalyticsErrors.ValidationError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_MixedValidAndDuplicateEvents_CorrectCounts()
    {
        var repository = new InMemoryActivationEventRepository();
        var handler = new RecordEventHandler(repository, new StubTimeProvider(NowUtc));
        var userId = Guid.NewGuid();

        // First insert signup_completed
        await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("signup_completed", null, null, NowUtc),
            ]),
            CancellationToken.None);

        // Now send both a duplicate and a new event
        var result = await handler.HandleAsync(
            new RecordEventCommand(userId, "ios",
            [
                new RecordEventItem("signup_completed", null, null, NowUtc),
                new RecordEventItem("household_created", Guid.NewGuid(), null, NowUtc),
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AcceptedCount);
        Assert.Equal(1, result.DuplicateCount);
    }
}

internal sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
