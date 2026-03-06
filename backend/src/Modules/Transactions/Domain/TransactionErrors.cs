namespace MoneyTracker.Modules.Transactions.Domain;

public static class TransactionErrors
{
    public const string ValidationError = "validation_error";
    public const string TransactionAmountInvalid = "transaction_amount_invalid";
    public const string TransactionDateOutOfRange = "transaction_date_out_of_range";
    public const string TransactionCategoryNotFound = "transaction_category_not_found";
    public const string TransactionHouseholdNotFound = "transaction_household_not_found";
    public const string TransactionAccessDenied = "transaction_access_denied";
}
