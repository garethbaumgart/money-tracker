using MoneyTracker.Modules.Budgets.Domain;

namespace MoneyTracker.Modules.Budgets.Application.GetBudgetCategories;

public sealed class GetBudgetCategoriesResult
{
    private GetBudgetCategoriesResult(
        IReadOnlyCollection<BudgetCategory>? categories,
        string? errorCode,
        string? errorMessage)
    {
        Categories = categories;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public IReadOnlyCollection<BudgetCategory>? Categories { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Categories is not null;

    public static GetBudgetCategoriesResult Success(IReadOnlyCollection<BudgetCategory> categories)
    {
        return new GetBudgetCategoriesResult(categories, errorCode: null, errorMessage: null);
    }

    public static GetBudgetCategoriesResult AccessDenied()
    {
        return new GetBudgetCategoriesResult(
            categories: null,
            BudgetErrors.BudgetAccessDenied,
            "User is not a member of this household.");
    }

    public static GetBudgetCategoriesResult HouseholdNotFound()
    {
        return new GetBudgetCategoriesResult(
            categories: null,
            BudgetErrors.BudgetHouseholdNotFound,
            "Household not found.");
    }
}
