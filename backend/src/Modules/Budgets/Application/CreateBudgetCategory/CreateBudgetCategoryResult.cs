using MoneyTracker.Modules.Budgets.Domain;

namespace MoneyTracker.Modules.Budgets.Application.CreateBudgetCategory;

public sealed class CreateBudgetCategoryResult
{
    private CreateBudgetCategoryResult(BudgetCategory? category, string? errorCode, string? errorMessage)
    {
        Category = category;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public BudgetCategory? Category { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Category is not null;

    public static CreateBudgetCategoryResult Success(BudgetCategory category)
    {
        return new CreateBudgetCategoryResult(category, errorCode: null, errorMessage: null);
    }

    public static CreateBudgetCategoryResult Conflict()
    {
        return new CreateBudgetCategoryResult(
            category: null,
            BudgetErrors.BudgetCategoryNameConflict,
            "A category with this name already exists.");
    }

    public static CreateBudgetCategoryResult Validation(string code, string message)
    {
        return new CreateBudgetCategoryResult(category: null, code, message);
    }

    public static CreateBudgetCategoryResult AccessDenied()
    {
        return new CreateBudgetCategoryResult(
            category: null,
            BudgetErrors.BudgetAccessDenied,
            "User is not a member of this household.");
    }

    public static CreateBudgetCategoryResult HouseholdNotFound()
    {
        return new CreateBudgetCategoryResult(
            category: null,
            BudgetErrors.BudgetHouseholdNotFound,
            "Household not found.");
    }
}
