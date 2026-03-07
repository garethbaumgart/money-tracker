using MoneyTracker.Modules.Transactions.Domain;

namespace MoneyTracker.Modules.Transactions.Application.GetTransactions;

public sealed class GetTransactionsResult
{
    private GetTransactionsResult(
        IReadOnlyCollection<TransactionSummary>? transactions,
        string? errorCode,
        string? errorMessage)
    {
        Transactions = transactions;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public IReadOnlyCollection<TransactionSummary>? Transactions { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public bool IsSuccess => Transactions is not null;

    public static GetTransactionsResult Success(IReadOnlyCollection<TransactionSummary> transactions)
    {
        return new GetTransactionsResult(transactions, errorCode: null, errorMessage: null);
    }

    public static GetTransactionsResult AccessDenied()
    {
        return new GetTransactionsResult(
            transactions: null,
            TransactionErrors.TransactionAccessDenied,
            "User is not a member of this household.");
    }

    public static GetTransactionsResult HouseholdNotFound()
    {
        return new GetTransactionsResult(
            transactions: null,
            TransactionErrors.TransactionHouseholdNotFound,
            "Household not found.");
    }
}

public sealed record TransactionSummary(
    Guid Id,
    Guid HouseholdId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    DateTimeOffset CreatedAtUtc);
