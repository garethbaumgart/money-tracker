using MoneyTracker.Modules.Budgets.Domain;

namespace MoneyTracker.Modules.Budgets.Application.UpsertBudgetAllocation;

public sealed class UpsertBudgetAllocationResult
{
    private UpsertBudgetAllocationResult(
        BudgetAllocation? allocation,
        string? errorCode,
        string? errorMessage)
    {
        Allocation = allocation;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public BudgetAllocation? Allocation { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Allocation is not null;

    public static UpsertBudgetAllocationResult Success(BudgetAllocation allocation)
    {
        return new UpsertBudgetAllocationResult(allocation, errorCode: null, errorMessage: null);
    }

    public static UpsertBudgetAllocationResult Validation(string code, string message)
    {
        return new UpsertBudgetAllocationResult(allocation: null, code, message);
    }

    public static UpsertBudgetAllocationResult CategoryNotFound()
    {
        return new UpsertBudgetAllocationResult(
            allocation: null,
            BudgetErrors.BudgetCategoryNotFound,
            "Category not found.");
    }

    public static UpsertBudgetAllocationResult AccessDenied()
    {
        return new UpsertBudgetAllocationResult(
            allocation: null,
            BudgetErrors.BudgetAccessDenied,
            "User is not a member of this household.");
    }

    public static UpsertBudgetAllocationResult HouseholdNotFound()
    {
        return new UpsertBudgetAllocationResult(
            allocation: null,
            BudgetErrors.BudgetHouseholdNotFound,
            "Household not found.");
    }
}
