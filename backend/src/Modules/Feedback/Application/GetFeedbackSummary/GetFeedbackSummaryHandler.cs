using MoneyTracker.Modules.Feedback.Domain;

namespace MoneyTracker.Modules.Feedback.Application.GetFeedbackSummary;

public sealed class GetFeedbackSummaryHandler(
    IFeedbackRepository feedbackRepository,
    INpsRepository npsRepository,
    TimeProvider timeProvider)
{
    public async Task<GetFeedbackSummaryResult> HandleAsync(
        GetFeedbackSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByPeriodAsync(
            query.PeriodStart,
            query.PeriodEnd,
            cancellationToken);

        var npsScores = await npsRepository.GetByPeriodAsync(
            query.PeriodStart,
            query.PeriodEnd,
            cancellationToken);

        var byCategory = feedback
            .GroupBy(f => f.Category.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var avgSatisfaction = feedback.Count > 0
            ? feedback.Average(f => f.Rating)
            : 0;

        // NPS = % Promoters - % Detractors
        var npsValue = 0.0;
        if (npsScores.Count > 0)
        {
            var promoters = npsScores.Count(s => s.Category == NpsCategory.Promoter);
            var detractors = npsScores.Count(s => s.Category == NpsCategory.Detractor);
            npsValue = ((double)(promoters - detractors) / npsScores.Count) * 100;
        }

        var priorityDistribution = feedback
            .GroupBy(f => f.PriorityScore.Bucket.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // AC-7: WoW trends
        var nowUtc = timeProvider.GetUtcNow();
        var oneWeekAgo = nowUtc.AddDays(-7);
        var twoWeeksAgo = nowUtc.AddDays(-14);

        var currentWeekCount = feedback.Count(f => f.CreatedAtUtc >= oneWeekAgo);
        var previousWeekCount = feedback.Count(f => f.CreatedAtUtc >= twoWeeksAgo && f.CreatedAtUtc < oneWeekAgo);

        var wowChangePercent = previousWeekCount > 0
            ? ((double)(currentWeekCount - previousWeekCount) / previousWeekCount) * 100
            : 0;

        var trends = new TrendData(currentWeekCount, previousWeekCount, wowChangePercent);

        var data = new FeedbackSummaryData(
            feedback.Count,
            byCategory,
            avgSatisfaction,
            npsValue,
            priorityDistribution,
            trends);

        return GetFeedbackSummaryResult.Success(data);
    }
}
