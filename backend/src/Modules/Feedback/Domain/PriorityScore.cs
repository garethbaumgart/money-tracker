namespace MoneyTracker.Modules.Feedback.Domain;

public sealed record PriorityScore(double Score, PriorityBucket Bucket)
{
    public static PriorityScore Compute(double score)
    {
        var bucket = score switch
        {
            >= 10 => PriorityBucket.Critical,
            >= 6 => PriorityBucket.High,
            >= 3 => PriorityBucket.Medium,
            _ => PriorityBucket.Low
        };

        return new PriorityScore(score, bucket);
    }
}

public enum PriorityBucket
{
    Low,
    Medium,
    High,
    Critical
}
