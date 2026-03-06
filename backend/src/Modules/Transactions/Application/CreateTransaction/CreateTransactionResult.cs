using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Application.CreateTransaction;

public sealed class CreateTransactionResult
{
    private CreateTransactionResult(Transaction? transaction, string? errorCode, string? errorMessage)
    {
        Transaction = transaction;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public Transaction? Transaction { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Transaction is not null;

    public static CreateTransactionResult Success(Transaction transaction)
    {
        return new CreateTransactionResult(transaction, errorCode: null, errorMessage: null);
    }

    public static CreateTransactionResult Validation(string code, string message)
    {
        return new CreateTransactionResult(transaction: null, code, message);
    }

    public static CreateTransactionResult CategoryNotFound()
    {
        return new CreateTransactionResult(
            transaction: null,
            TransactionErrors.TransactionCategoryNotFound,
            "Category not found.");
    }

    public static CreateTransactionResult AccessDenied()
    {
        return new CreateTransactionResult(
            transaction: null,
            TransactionErrors.TransactionAccessDenied,
            "User is not a member of this household.");
    }

    public static CreateTransactionResult HouseholdNotFound()
    {
        return new CreateTransactionResult(
            transaction: null,
            TransactionErrors.TransactionHouseholdNotFound,
            "Household not found.");
    }
}
