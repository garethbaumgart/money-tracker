namespace MoneyTracker.Modules.Subscriptions.Application.RestorePurchases;

public sealed record RestorePurchasesCommand(
    Guid HouseholdId,
    Guid UserId,
    string RevenueCatAppUserId);
