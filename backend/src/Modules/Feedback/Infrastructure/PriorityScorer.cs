using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Infrastructure;

public sealed class PriorityScorer
{
    private static readonly string[] CrashWords = ["crash", "crashes", "crashing", "crashed", "freeze", "frozen", "hung", "unresponsive"];
    private static readonly string[] DataLossWords = ["lost data", "data loss", "lost my", "deleted", "missing transactions", "wiped", "disappeared", "gone"];

    /// <summary>
    /// Computes a priority score for a feedback item.
    /// Formula: base score (Bug=3, Feature=1, General=1)
    ///          * tier multiplier (Paid=2.0, Trial=1.5, Free=1.0)
    ///          + frequency bonus (+1 per similar in 7 days)
    ///          + severity bonus (+2 crash words, +3 data loss words)
    /// </summary>
    public PriorityScore ComputeScore(
        FeedbackCategory category,
        string description,
        string userTier,
        int similarCountLast7Days)
    {
        double baseScore = category switch
        {
            FeedbackCategory.Bug => 3,
            FeedbackCategory.Feature => 1,
            FeedbackCategory.General => 1,
            _ => 1
        };

        double tierMultiplier = userTier.ToLowerInvariant() switch
        {
            "paid" or "premium" => 2.0,
            "trial" => 1.5,
            _ => 1.0
        };

        double frequencyBonus = similarCountLast7Days;

        double severityBonus = 0;
        var lowerDescription = description.ToLowerInvariant();

        if (CrashWords.Any(word => lowerDescription.Contains(word)))
        {
            severityBonus += 2;
        }

        if (DataLossWords.Any(phrase => lowerDescription.Contains(phrase)))
        {
            severityBonus += 3;
        }

        var totalScore = (baseScore * tierMultiplier) + frequencyBonus + severityBonus;

        return PriorityScore.Compute(totalScore);
    }
}
