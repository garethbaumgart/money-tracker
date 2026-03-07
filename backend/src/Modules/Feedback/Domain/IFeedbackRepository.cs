namespace MoneyTracker.Modules.Feedback.Domain;

public interface IFeedbackRepository
{
    Task AddAsync(FeedbackItem feedback, CancellationToken cancellationToken);
    Task UpdateAsync(FeedbackItem feedback, CancellationToken cancellationToken);
    Task<FeedbackItem?> GetByIdAsync(FeedbackId id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackItem>> GetByUserSinceAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackItem>> GetByPeriodAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
    Task<int> CountSimilarInPeriodAsync(
        FeedbackCategory category,
        DateTimeOffset since,
        CancellationToken cancellationToken);
}
