using MoneyTracker.Modules.Feedback.Application.TriageFeedback;
using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.Feedback.Infrastructure;

namespace MoneyTracker.Modules.Feedback.Tests.Application;

public sealed class TriageFeedbackHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ExistingFeedback_TriagesSuccessfully()
    {
        var repo = new InMemoryFeedbackRepository();
        var timeProvider = new FakeTimeProvider(NowUtc);
        var handler = new TriageFeedbackHandler(repo, timeProvider);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Bug,
            "Some bug",
            3,
            new FeedbackMetadata(null, null, null, null),
            PriorityScore.Compute(5),
            NowUtc);
        await repo.AddAsync(feedback, CancellationToken.None);

        var result = await handler.HandleAsync(
            new TriageFeedbackCommand(feedback.Id, FeedbackStatus.Triaged, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_FeedbackNotFound_ReturnsNotFound()
    {
        var repo = new InMemoryFeedbackRepository();
        var timeProvider = new FakeTimeProvider(NowUtc);
        var handler = new TriageFeedbackHandler(repo, timeProvider);

        var result = await handler.HandleAsync(
            new TriageFeedbackCommand(FeedbackId.New(), FeedbackStatus.Triaged, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.NotFound, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithPriorityOverride_UpdatesPriority()
    {
        var repo = new InMemoryFeedbackRepository();
        var timeProvider = new FakeTimeProvider(NowUtc);
        var handler = new TriageFeedbackHandler(repo, timeProvider);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Feature,
            "Some feature request",
            4,
            new FeedbackMetadata(null, null, null, null),
            PriorityScore.Compute(2),
            NowUtc);
        await repo.AddAsync(feedback, CancellationToken.None);

        var result = await handler.HandleAsync(
            new TriageFeedbackCommand(feedback.Id, FeedbackStatus.Triaged, 10.0),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PriorityBucket.Critical, feedback.PriorityScore.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_TriageBackToNew_ReturnsValidationError()
    {
        var repo = new InMemoryFeedbackRepository();
        var timeProvider = new FakeTimeProvider(NowUtc);
        var handler = new TriageFeedbackHandler(repo, timeProvider);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Bug,
            "Some bug",
            3,
            new FeedbackMetadata(null, null, null, null),
            PriorityScore.Compute(5),
            NowUtc);
        await repo.AddAsync(feedback, CancellationToken.None);

        var result = await handler.HandleAsync(
            new TriageFeedbackCommand(feedback.Id, FeedbackStatus.New, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.InvalidStatusTransition, result.ErrorCode);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
