using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Analytics;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.Budgets.Application.CreateBudgetCategory;

public sealed class CreateBudgetCategoryHandler(
    IBudgetRepository repository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider,
    IAnalyticsEventPublisher analyticsPublisher)
{
    public async Task<CreateBudgetCategoryResult> HandleAsync(
        CreateBudgetCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return CreateBudgetCategoryResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return CreateBudgetCategoryResult.AccessDenied();
        }

        BudgetCategory category;
        try
        {
            category = BudgetCategory.Create(
                command.HouseholdId,
                command.Name,
                command.RequestingUserId,
                timeProvider.GetUtcNow());
        }
        catch (BudgetDomainException exception)
        {
            return CreateBudgetCategoryResult.Validation(exception.Code, exception.Message);
        }

        var added = await repository.AddCategoryAsync(category, cancellationToken);
        if (!added)
        {
            return CreateBudgetCategoryResult.Conflict();
        }

        await analyticsPublisher.PublishAsync(
            command.RequestingUserId, "first_budget_created", command.HouseholdId, cancellationToken);

        return CreateBudgetCategoryResult.Success(category);
    }
}
