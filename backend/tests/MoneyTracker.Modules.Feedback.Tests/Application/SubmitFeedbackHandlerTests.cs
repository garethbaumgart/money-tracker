using MoneyTracker.Modules.Feedback.Application.SubmitFeedback;
using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.Feedback.Infrastructure;

namespace MoneyTracker.Modules.Feedback.Tests.Application;

public sealed class SubmitFeedbackHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    private static (SubmitFeedbackHandler handler, InMemoryFeedbackRepository repo) CreateHandler()
    {
        var repo = new InMemoryFeedbackRepository();
        var scorer = new PriorityScorer();
        var timeProvider = new FakeTimeProvider(NowUtc);
        var handler = new SubmitFeedbackHandler(repo, scorer, timeProvider);
        return (handler, repo);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ValidFeedback_ReturnsSuccess()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new SubmitFeedbackCommand(
                Guid.NewGuid(),
                FeedbackCategory.Bug,
                "The settings page crashes",
                4,
                "settings",
                "1.0.0",
                "iPhone 15",
                "iOS 18",
                "free"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.FeedbackId);
        Assert.Equal("New", result.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_EmptyDescription_ReturnsValidationError()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new SubmitFeedbackCommand(
                Guid.NewGuid(),
                FeedbackCategory.General,
                "",
                3,
                null, null, null, null,
                "free"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.ValidationError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_RatingOutOfRange_ReturnsValidationError()
    {
        var (handler, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new SubmitFeedbackCommand(
                Guid.NewGuid(),
                FeedbackCategory.General,
                "Some feedback",
                0,
                null, null, null, null,
                "free"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.ValidationError, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ExceedsRateLimit_ReturnsRateLimited()
    {
        var (handler, _) = CreateHandler();
        var userId = Guid.NewGuid();

        // Submit 5 feedback items
        for (int i = 0; i < 5; i++)
        {
            var result = await handler.HandleAsync(
                new SubmitFeedbackCommand(
                    userId,
                    FeedbackCategory.General,
                    $"Feedback #{i + 1}",
                    3,
                    null, null, null, null,
                    "free"),
                CancellationToken.None);
            Assert.True(result.IsSuccess);
        }

        // 6th should be rate limited
        var rateLimitedResult = await handler.HandleAsync(
            new SubmitFeedbackCommand(
                userId,
                FeedbackCategory.General,
                "One more feedback",
                3,
                null, null, null, null,
                "free"),
            CancellationToken.None);

        Assert.False(rateLimitedResult.IsSuccess);
        Assert.Equal(FeedbackErrors.RateLimitExceeded, rateLimitedResult.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_DifferentUsers_NoRateLimit()
    {
        var (handler, _) = CreateHandler();

        // Each different user should succeed
        for (int i = 0; i < 10; i++)
        {
            var result = await handler.HandleAsync(
                new SubmitFeedbackCommand(
                    Guid.NewGuid(),
                    FeedbackCategory.General,
                    $"Feedback #{i + 1}",
                    3,
                    null, null, null, null,
                    "free"),
                CancellationToken.None);
            Assert.True(result.IsSuccess);
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
