namespace MoneyTracker.Modules.Transactions.Application.GetTransactions;

public sealed record GetTransactionsQuery(
    Guid HouseholdId,
    Guid RequestingUserId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);
