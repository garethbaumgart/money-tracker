using MoneyTracker.Modules.Feedback.Domain;
using MoneyTracker.Modules.Feedback.Infrastructure;

namespace MoneyTracker.Modules.Feedback.Application.SubmitFeedback;

public sealed class SubmitFeedbackHandler(
    IFeedbackRepository feedbackRepository,
    PriorityScorer priorityScorer,
    TimeProvider timeProvider)
{
    private const int MaxFeedbackPerDay = 5;

    public async Task<SubmitFeedbackResult> HandleAsync(
        SubmitFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();

        // AC-15: Rate limit — 5 per user per 24h
        var recentFeedback = await feedbackRepository.GetByUserSinceAsync(
            command.UserId,
            nowUtc.AddHours(-24),
            cancellationToken);

        if (recentFeedback.Count >= MaxFeedbackPerDay)
        {
            return SubmitFeedbackResult.RateLimited();
        }

        // AC-3: Auto-computed priority score
        var similarCount = await feedbackRepository.CountSimilarInPeriodAsync(
            command.Category,
            nowUtc.AddDays(-7),
            cancellationToken);

        var priorityScore = priorityScorer.ComputeScore(
            command.Category,
            command.Description,
            command.UserTier,
            similarCount);

        var metadata = new FeedbackMetadata(
            command.ScreenName,
            command.AppVersion,
            command.DeviceModel,
            command.OsVersion);

        FeedbackItem feedback;
        try
        {
            feedback = FeedbackItem.Create(
                command.UserId,
                command.Category,
                command.Description,
                command.Rating,
                metadata,
                priorityScore,
                nowUtc);
        }
        catch (FeedbackDomainException ex)
        {
            return SubmitFeedbackResult.Validation(ex.Code, ex.Message);
        }

        await feedbackRepository.AddAsync(feedback, cancellationToken);

        return SubmitFeedbackResult.Success(feedback.Id.Value, feedback.Status.ToString());
    }
}
