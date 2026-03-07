namespace MoneyTracker.Modules.Subscriptions.Application.GetEntitlements;

public sealed record GetEntitlementsQuery(Guid HouseholdId, Guid UserId);
