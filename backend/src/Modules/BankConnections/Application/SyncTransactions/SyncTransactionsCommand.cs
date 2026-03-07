namespace MoneyTracker.Modules.BankConnections.Application.SyncTransactions;

public sealed record SyncTransactionsCommand(Guid? HouseholdId);
