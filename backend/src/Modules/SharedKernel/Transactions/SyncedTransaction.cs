namespace MoneyTracker.Modules.SharedKernel.Transactions;

public sealed record SyncedTransaction(
    Guid HouseholdId,
    Guid BankConnectionId,
    string ExternalTransactionId,
    decimal Amount,
    DateTimeOffset OccurredAtUtc,
    string? Description,
    DateTimeOffset CreatedAtUtc);
