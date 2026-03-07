using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.Budgets.Application.GetBudgetCategories;

public sealed class GetBudgetCategoriesHandler(
    IBudgetRepository repository,
    IHouseholdAccessService householdAccessService)
{
    public async Task<GetBudgetCategoriesResult> HandleAsync(
        GetBudgetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            query.HouseholdId,
            query.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return GetBudgetCategoriesResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return GetBudgetCategoriesResult.AccessDenied();
        }

        var categories = await repository.GetCategoriesAsync(query.HouseholdId, cancellationToken);
        var ordered = categories.OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        return GetBudgetCategoriesResult.Success(ordered);
    }
}
