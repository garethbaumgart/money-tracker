using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.GetSubscription;

public sealed class GetSubscriptionHandler(
    ISubscriptionRepository repository)
{
    public async Task<GetSubscriptionResult> HandleAsync(
        GetSubscriptionQuery query,
        CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByHouseholdIdAsync(
            query.HouseholdId,
            cancellationToken);

        if (subscription is null)
        {
            return GetSubscriptionResult.NotFound();
        }

        var summary = new SubscriptionSummary(
            subscription.Id.Value,
            subscription.HouseholdId,
            subscription.RevenueCatAppUserId,
            subscription.ProductId,
            subscription.Status.ToString(),
            subscription.CurrentPeriodStartUtc,
            subscription.CurrentPeriodEndUtc,
            subscription.CancelledAtUtc,
            subscription.BillingIssueDetectedAtUtc,
            subscription.OriginalPurchaseDateUtc,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);

        return GetSubscriptionResult.Success(summary);
    }
}
