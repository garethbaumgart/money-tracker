namespace MoneyTracker.Modules.BankConnections.Application.CreateLinkSession;

public sealed record CreateLinkSessionCommand(
    Guid HouseholdId,
    Guid RequestingUserId);
