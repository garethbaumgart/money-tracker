namespace MoneyTracker.Modules.SharedKernel.Analytics;

public interface IAnalyticsEventPublisher
{
    Task PublishAsync(Guid userId, string milestone, Guid? householdId, CancellationToken cancellationToken);
}
