using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.Feedback.Infrastructure;

namespace MoneyTracker.Modules.Feedback.Tests.Infrastructure;

public sealed class PriorityScorerTests
{
    private readonly PriorityScorer _scorer = new();

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_Bug_FreeTier_NoBonus_ReturnsBaseScore()
    {
        // Bug base = 3, Free multiplier = 1.0, no frequency, no severity
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "Something is wrong", "free", 0);

        Assert.Equal(3.0, result.Score);
        Assert.Equal(PriorityBucket.Medium, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_Bug_PaidTier_ReturnsMultiplied()
    {
        // Bug base = 3, Paid multiplier = 2.0 => 6.0
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "Something is wrong", "paid", 0);

        Assert.Equal(6.0, result.Score);
        Assert.Equal(PriorityBucket.High, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_Bug_TrialTier_ReturnsMultiplied()
    {
        // Bug base = 3, Trial multiplier = 1.5 => 4.5
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "Something is wrong", "trial", 0);

        Assert.Equal(4.5, result.Score);
        Assert.Equal(PriorityBucket.Medium, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_Feature_FreeTier_ReturnsLowScore()
    {
        // Feature base = 1, Free multiplier = 1.0 => 1.0
        var result = _scorer.ComputeScore(FeedbackCategory.Feature, "Add dark mode", "free", 0);

        Assert.Equal(1.0, result.Score);
        Assert.Equal(PriorityBucket.Low, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_WithFrequencyBonus_AddsToScore()
    {
        // Bug base = 3, Free multiplier = 1.0 => 3.0 + 5 frequency = 8.0
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "Something is wrong", "free", 5);

        Assert.Equal(8.0, result.Score);
        Assert.Equal(PriorityBucket.High, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_CrashKeyword_AddsSeverityBonus()
    {
        // Bug base = 3, Free multiplier = 1.0 => 3.0 + 2 crash bonus = 5.0
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "The app crashes on startup", "free", 0);

        Assert.Equal(5.0, result.Score);
        Assert.Equal(PriorityBucket.Medium, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_DataLossKeyword_AddsSeverityBonus()
    {
        // Bug base = 3, Free multiplier = 1.0 => 3.0 + 3 data loss bonus = 6.0
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "I lost my transactions", "free", 0);

        Assert.Equal(6.0, result.Score);
        Assert.Equal(PriorityBucket.High, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_BothCrashAndDataLoss_AddsBothBonuses()
    {
        // Bug base = 3, Free multiplier = 1.0 => 3.0 + 2 crash + 3 data loss = 8.0
        var result = _scorer.ComputeScore(FeedbackCategory.Bug, "App crashed and I lost data", "free", 0);

        Assert.Equal(8.0, result.Score);
        Assert.Equal(PriorityBucket.High, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_AllFactors_CriticalPriority()
    {
        // Bug base = 3, Paid multiplier = 2.0 => 6.0 + 3 frequency + 2 crash + 3 data loss = 14.0
        var result = _scorer.ComputeScore(
            FeedbackCategory.Bug,
            "App crashed and data loss occurred",
            "premium",
            3);

        Assert.Equal(14.0, result.Score);
        Assert.Equal(PriorityBucket.Critical, result.Bucket);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeScore_PremiumTier_SameAsPaid()
    {
        var paidResult = _scorer.ComputeScore(FeedbackCategory.Bug, "test", "paid", 0);
        var premiumResult = _scorer.ComputeScore(FeedbackCategory.Bug, "test", "premium", 0);

        Assert.Equal(paidResult.Score, premiumResult.Score);
    }
}
