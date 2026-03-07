namespace MoneyTracker.Modules.Budgets.Domain;

public static class BudgetErrors
{
    public const string ValidationError = "validation_error";
    public const string BudgetCategoryNameRequired = "budget_category_name_required";
    public const string BudgetCategoryNameConflict = "budget_category_name_conflict";
    public const string BudgetCategoryNotFound = "budget_category_not_found";
    public const string BudgetAllocationAmountInvalid = "budget_allocation_amount_invalid";
    public const string BudgetPeriodInvalid = "budget_period_invalid";
    public const string BudgetHouseholdNotFound = "budget_household_not_found";
    public const string BudgetAccessDenied = "budget_access_denied";
}
