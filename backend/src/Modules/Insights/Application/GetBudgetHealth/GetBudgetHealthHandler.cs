using MoneyTracker.Modules.Insights.Domain;
using MoneyTracker.Modules.SharedKernel.Households;
using MoneyTracker.Modules.SharedKernel.Subscriptions;
using MoneyTracker.Modules.Transactions.Domain;
using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.BillReminders.Domain;

namespace MoneyTracker.Modules.Insights.Application.GetBudgetHealth;

public sealed class GetBudgetHealthHandler(
    ISubscriptionEntitlementService entitlementService,
    IHouseholdAccessService householdAccessService,
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    IBillReminderRepository billReminderRepository,
    TimeProvider timeProvider)
{
    public async Task<GetBudgetHealthResult> HandleAsync(
        GetBudgetHealthQuery query,
        CancellationToken cancellationToken)
    {
        // Check household membership
        var accessResult = await householdAccessService.CheckMemberAsync(
            query.HouseholdId, query.UserId, cancellationToken);

        if (!accessResult.HouseholdExists)
        {
            return GetBudgetHealthResult.Error(
                InsightsErrors.HouseholdNotFound,
                "The household was not found.");
        }

        if (!accessResult.IsMember)
        {
            return GetBudgetHealthResult.Error(
                InsightsErrors.AccessDenied,
                "You do not have access to this household.");
        }

        // Check entitlement
        var isAllowed = await entitlementService.IsFeatureAllowedAsync(
            query.HouseholdId, InsightsPolicy.FeatureKey, cancellationToken);

        if (!isAllowed)
        {
            return GetBudgetHealthResult.Error(
                InsightsErrors.PremiumRequired,
                "Premium subscription is required to access insights.");
        }

        var now = timeProvider.GetUtcNow();
        var periodStart = BudgetPeriod.GetPeriodStart(now);
        var periodEnd = BudgetPeriod.GetPeriodEnd(periodStart);

        // Get allocations for the current budget period
        var allocations = await budgetRepository.GetAllocationsAsync(
            query.HouseholdId, periodStart, cancellationToken);

        // Get categories for name lookup
        var categories = await budgetRepository.GetCategoriesAsync(
            query.HouseholdId, cancellationToken);
        var categoryLookup = categories.ToDictionary(c => c.Id.Value, c => c.Name);

        // Get transactions for the current budget period
        var transactions = await transactionRepository.GetByHouseholdAsync(
            query.HouseholdId, periodStart, periodEnd, cancellationToken);

        // Aggregate spending by category
        var spendingByCategory = transactions
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        // Build category budget data
        var categoryData = allocations
            .Select(a => new CategoryBudgetData(
                a.CategoryId.Value,
                categoryLookup.GetValueOrDefault(a.CategoryId.Value, "Unknown"),
                a.Amount,
                spendingByCategory.GetValueOrDefault(a.CategoryId.Value, 0m)))
            .ToArray();

        var totalAllocated = allocations.Sum(a => a.Amount);
        var totalSpent = transactions.Sum(t => t.Amount);

        // Calculate days remaining
        var totalDaysInPeriod = (int)(periodEnd - periodStart).TotalDays;
        var daysRemaining = Math.Max(0, (int)(periodEnd - now).TotalDays);

        // Get bill reminders for bill payment score
        var billReminders = await billReminderRepository.GetByHouseholdAsync(
            query.HouseholdId, cancellationToken);

        var totalBillsDue = billReminders.Count;
        var billsPaidOnTime = billReminders.Count(b => !b.IsOverdue(now));

        var healthScore = BudgetHealthScore.Compute(
            categoryData,
            totalAllocated,
            totalSpent,
            daysRemaining,
            totalDaysInPeriod,
            billsPaidOnTime,
            totalBillsDue);

        return GetBudgetHealthResult.Success(healthScore, periodStart, periodEnd);
    }
}
