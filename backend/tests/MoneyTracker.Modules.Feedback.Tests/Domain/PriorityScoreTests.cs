using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Tests.Domain;

public sealed class PriorityScoreTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(10, PriorityBucket.Critical)]
    [InlineData(15, PriorityBucket.Critical)]
    [InlineData(6, PriorityBucket.High)]
    [InlineData(9.9, PriorityBucket.High)]
    [InlineData(3, PriorityBucket.Medium)]
    [InlineData(5.9, PriorityBucket.Medium)]
    [InlineData(0, PriorityBucket.Low)]
    [InlineData(2.9, PriorityBucket.Low)]
    public void Compute_AssignsCorrectBucket(double score, PriorityBucket expectedBucket)
    {
        var priority = PriorityScore.Compute(score);

        Assert.Equal(expectedBucket, priority.Bucket);
        Assert.Equal(score, priority.Score);
    }
}
