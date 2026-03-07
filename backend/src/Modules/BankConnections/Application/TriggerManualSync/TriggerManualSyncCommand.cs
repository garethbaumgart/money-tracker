namespace MoneyTracker.Modules.BankConnections.Application.TriggerManualSync;

public sealed record TriggerManualSyncCommand(
    Guid HouseholdId,
    Guid RequestingUserId);
