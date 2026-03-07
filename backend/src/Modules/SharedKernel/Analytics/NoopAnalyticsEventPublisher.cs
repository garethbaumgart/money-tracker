namespace MoneyTracker.Modules.SharedKernel.Analytics;

public sealed class NoopAnalyticsEventPublisher : IAnalyticsEventPublisher
{
    public Task PublishAsync(Guid userId, string milestone, Guid? householdId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
