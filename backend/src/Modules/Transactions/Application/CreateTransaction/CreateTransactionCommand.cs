namespace MoneyTracker.Modules.Transactions.Application.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid HouseholdId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    Guid? CategoryId,
    Guid RequestingUserId);
