using MoneyTracker.Modules.Feedback.Application.SubmitNps;
using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.Feedback.Infrastructure;

namespace MoneyTracker.Modules.Feedback.Tests.Application;

public sealed class SubmitNpsHandlerTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ValidScore_ReturnsSuccess()
    {
        var repo = new InMemoryNpsRepository();
        var handler = new SubmitNpsHandler(repo, new FakeTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new SubmitNpsCommand(Guid.NewGuid(), 9, "Love it!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.NpsId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ScoreOutOfRange_ReturnsValidationError()
    {
        var repo = new InMemoryNpsRepository();
        var handler = new SubmitNpsHandler(repo, new FakeTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new SubmitNpsCommand(Guid.NewGuid(), 11, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.NpsScoreOutOfRange, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_NegativeScore_ReturnsValidationError()
    {
        var repo = new InMemoryNpsRepository();
        var handler = new SubmitNpsHandler(repo, new FakeTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new SubmitNpsCommand(Guid.NewGuid(), -1, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(FeedbackErrors.NpsScoreOutOfRange, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_NullComment_Succeeds()
    {
        var repo = new InMemoryNpsRepository();
        var handler = new SubmitNpsHandler(repo, new FakeTimeProvider(NowUtc));

        var result = await handler.HandleAsync(
            new SubmitNpsCommand(Guid.NewGuid(), 5, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
