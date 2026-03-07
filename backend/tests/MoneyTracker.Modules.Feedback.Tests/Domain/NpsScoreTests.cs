using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Tests.Domain;

public sealed class NpsScoreTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(10, NpsCategory.Promoter)]
    [InlineData(9, NpsCategory.Promoter)]
    [InlineData(8, NpsCategory.Passive)]
    [InlineData(7, NpsCategory.Passive)]
    [InlineData(6, NpsCategory.Detractor)]
    [InlineData(0, NpsCategory.Detractor)]
    public void Create_ValidScore_AssignsCorrectCategory(int score, NpsCategory expectedCategory)
    {
        var nps = NpsScore.Create(Guid.NewGuid(), score, null, NowUtc);

        Assert.Equal(expectedCategory, nps.Category);
        Assert.Equal(score, nps.Score);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(-1)]
    [InlineData(11)]
    public void Create_InvalidScore_ThrowsDomainException(int score)
    {
        var exception = Assert.Throws<FeedbackDomainException>(
            () => NpsScore.Create(Guid.NewGuid(), score, null, NowUtc));

        Assert.Equal(FeedbackErrors.NpsScoreOutOfRange, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_WithComment_StoresComment()
    {
        var nps = NpsScore.Create(Guid.NewGuid(), 8, "Great app!", NowUtc);

        Assert.Equal("Great app!", nps.Comment);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_CommentExceedsMaxLength_ThrowsDomainException()
    {
        var longComment = new string('x', 1001);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => NpsScore.Create(Guid.NewGuid(), 8, longComment, NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_EmptyUserId_ThrowsDomainException()
    {
        var exception = Assert.Throws<FeedbackDomainException>(
            () => NpsScore.Create(Guid.Empty, 8, null, NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
    }
}
