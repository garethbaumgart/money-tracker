using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Tests.Domain;

public sealed class FeedbackItemTests
{
    private static readonly DateTimeOffset NowUtc = DateTimeOffset.Parse("2026-03-01T00:00:00Z");

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_ValidInputs_Succeeds()
    {
        var metadata = new FeedbackMetadata("home", "1.0.0", "iPhone 15", "iOS 18");
        var priority = PriorityScore.Compute(5);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Bug,
            "The app crashes when I open settings",
            4,
            metadata,
            priority,
            NowUtc);

        Assert.Equal(FeedbackCategory.Bug, feedback.Category);
        Assert.Equal("The app crashes when I open settings", feedback.Description);
        Assert.Equal(4, feedback.Rating);
        Assert.Equal(FeedbackStatus.New, feedback.Status);
        Assert.Equal(NowUtc, feedback.CreatedAtUtc);
        Assert.Null(feedback.TriagedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_EmptyDescription_ThrowsDomainException()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(1);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => FeedbackItem.Create(
                Guid.NewGuid(),
                FeedbackCategory.General,
                "",
                3,
                metadata,
                priority,
                NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_DescriptionExceedsMaxLength_ThrowsDomainException()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(1);
        var longDescription = new string('x', 5001);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => FeedbackItem.Create(
                Guid.NewGuid(),
                FeedbackCategory.General,
                longDescription,
                3,
                metadata,
                priority,
                NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
        Assert.Contains("5000", exception.Message);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_InvalidRating_ThrowsDomainException(int rating)
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(1);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => FeedbackItem.Create(
                Guid.NewGuid(),
                FeedbackCategory.General,
                "Some feedback",
                rating,
                metadata,
                priority,
                NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
        Assert.Contains("between 1 and 5", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Create_EmptyUserId_ThrowsDomainException()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(1);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => FeedbackItem.Create(
                Guid.Empty,
                FeedbackCategory.General,
                "Some feedback",
                3,
                metadata,
                priority,
                NowUtc));

        Assert.Equal(FeedbackErrors.ValidationError, exception.Code);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Triage_ToTriaged_UpdatesStatusAndTimestamp()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(5);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Bug,
            "Some bug",
            3,
            metadata,
            priority,
            NowUtc);

        var triagedAt = NowUtc.AddHours(1);
        feedback.Triage(FeedbackStatus.Triaged, null, triagedAt);

        Assert.Equal(FeedbackStatus.Triaged, feedback.Status);
        Assert.Equal(triagedAt, feedback.TriagedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Triage_WithPriorityOverride_UpdatesPriority()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(2);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Feature,
            "Need dark mode",
            5,
            metadata,
            priority,
            NowUtc);

        var override_ = PriorityScore.Compute(10);
        feedback.Triage(FeedbackStatus.Triaged, override_, NowUtc.AddHours(1));

        Assert.Equal(PriorityBucket.Critical, feedback.PriorityScore.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Triage_BackToNew_ThrowsDomainException()
    {
        var metadata = new FeedbackMetadata(null, null, null, null);
        var priority = PriorityScore.Compute(5);

        var feedback = FeedbackItem.Create(
            Guid.NewGuid(),
            FeedbackCategory.Bug,
            "Some bug",
            3,
            metadata,
            priority,
            NowUtc);

        var exception = Assert.Throws<FeedbackDomainException>(
            () => feedback.Triage(FeedbackStatus.New, null, NowUtc.AddHours(1)));

        Assert.Equal(FeedbackErrors.InvalidStatusTransition, exception.Code);
    }
}
