namespace MoneyTracker.Modules.BankConnections.Application.GetBankConnections;

public sealed record GetBankConnectionsQuery(
    Guid HouseholdId,
    Guid RequestingUserId);
