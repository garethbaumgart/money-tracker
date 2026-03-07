using MoneyTracker.Modules.Budgets.Domain;
using MoneyTracker.Modules.SharedKernel.Households;

namespace MoneyTracker.Modules.Budgets.Application.UpsertBudgetAllocation;

public sealed class UpsertBudgetAllocationHandler(
    IBudgetRepository repository,
    IHouseholdAccessService householdAccessService,
    TimeProvider timeProvider)
{
    public async Task<UpsertBudgetAllocationResult> HandleAsync(
        UpsertBudgetAllocationCommand command,
        CancellationToken cancellationToken)
    {
        var access = await householdAccessService.CheckMemberAsync(
            command.HouseholdId,
            command.RequestingUserId,
            cancellationToken);
        if (!access.HouseholdExists)
        {
            return UpsertBudgetAllocationResult.HouseholdNotFound();
        }

        if (!access.IsMember)
        {
            return UpsertBudgetAllocationResult.AccessDenied();
        }

        var categoryId = new BudgetCategoryId(command.CategoryId);
        var category = await repository.GetCategoryAsync(command.HouseholdId, categoryId, cancellationToken);
        if (category is null)
        {
            return UpsertBudgetAllocationResult.CategoryNotFound();
        }

        var nowUtc = timeProvider.GetUtcNow();
        var periodStartUtc = command.PeriodStartUtc?.ToUniversalTime() ?? BudgetPeriod.GetPeriodStart(nowUtc);
        if (!BudgetPeriod.IsPeriodStart(periodStartUtc))
        {
            periodStartUtc = BudgetPeriod.GetPeriodStart(periodStartUtc);
        }

        if (periodStartUtc < nowUtc.AddMonths(-12) || periodStartUtc > nowUtc.AddMonths(12))
        {
            return UpsertBudgetAllocationResult.Validation(
                BudgetErrors.BudgetPeriodInvalid,
                "Budget period must be within 12 months of the current date.");
        }

        BudgetAllocation allocation;
        try
        {
            var existing = await repository.GetAllocationAsync(
                command.HouseholdId,
                categoryId,
                periodStartUtc,
                cancellationToken);
            if (existing is not null)
            {
                existing.UpdateAmount(command.Amount, nowUtc);
                allocation = existing;
            }
            else
            {
                allocation = BudgetAllocation.Create(
                    command.HouseholdId,
                    categoryId,
                    command.Amount,
                    periodStartUtc,
                    command.RequestingUserId,
                    nowUtc);
            }
        }
        catch (BudgetDomainException exception)
        {
            return UpsertBudgetAllocationResult.Validation(exception.Code, exception.Message);
        }

        var saved = await repository.UpsertAllocationAsync(allocation, cancellationToken);
        return UpsertBudgetAllocationResult.Success(saved);
    }
}
