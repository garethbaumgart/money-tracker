using MoneyTracker.Modules.Insights.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Subscriptions;
using MoneyTracker.Modules.Transactions.Domain;
using MoneyTracker.Modules.Budgets.Domain;

namespace MoneyTracker.Modules.Insights.Application.GetSpendingSummary;

public sealed class GetSpendingSummaryHandler(
    ISubscriptionEntitlementService entitlementService,
    IHouseholdAccessService householdAccessService,
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    TimeProvider timeProvider)
{
    public async Task<GetSpendingSummaryResult> HandleAsync(
        GetSpendingSummaryQuery query,
        CancellationToken cancellationToken)
    {
        // Check household membership
        var accessResult = await householdAccessService.CheckMemberAsync(
            query.HouseholdId, query.UserId, cancellationToken);

        if (!accessResult.HouseholdExists)
        {
            return GetSpendingSummaryResult.Error(
                InsightsErrors.HouseholdNotFound,
                "The household was not found.");
        }

        if (!accessResult.IsMember)
        {
            return GetSpendingSummaryResult.Error(
                InsightsErrors.AccessDenied,
                "You do not have access to this household.");
        }

        // Check entitlement
        var isAllowed = await entitlementService.IsFeatureAllowedAsync(
            query.HouseholdId, InsightsPolicy.FeatureKey, cancellationToken);

        if (!isAllowed)
        {
            return GetSpendingSummaryResult.Error(
                InsightsErrors.PremiumRequired,
                "Premium subscription is required to access insights.");
        }

        var now = timeProvider.GetUtcNow();
        var days = query.Period.ToDays();
        var periodEnd = now;
        var periodStart = now.AddDays(-days);
        var previousPeriodEnd = periodStart;
        var previousPeriodStart = periodStart.AddDays(-days);

        // Get transactions for current and previous periods
        var currentTransactions = await transactionRepository.GetByHouseholdAsync(
            query.HouseholdId, periodStart, periodEnd, cancellationToken);

        var previousTransactions = await transactionRepository.GetByHouseholdAsync(
            query.HouseholdId, previousPeriodStart, previousPeriodEnd, cancellationToken);

        // Get categories
        var categories = await budgetRepository.GetCategoriesAsync(
            query.HouseholdId, cancellationToken);

        var categoryLookup = categories.ToDictionary(
            c => c.Id.Value,
            c => c.Name);

        // Aggregate current period spending by category
        var currentData = AggregateByCategory(currentTransactions, categoryLookup);
        var previousData = AggregateByCategory(previousTransactions, categoryLookup);

        var analysis = SpendingAnalysis.Compute(
            currentData,
            previousData,
            InsightsPolicy.DefaultAnomalyThresholdPercent);

        return GetSpendingSummaryResult.Success(analysis, periodStart, periodEnd);
    }

    private static IReadOnlyCollection<CategorySpendingData> AggregateByCategory(
        IReadOnlyCollection<Transaction> transactions,
        Dictionary<Guid, string> categoryLookup)
    {
        return transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new CategorySpendingData(
                g.Key,
                categoryLookup.GetValueOrDefault(g.Key, "Unknown"),
                g.Sum(t => t.Amount)))
            .ToArray();
    }
}
