using MoneyTracker.Modules.Subscriptions.Domain;

namespace MoneyTracker.Modules.Subscriptions.Application.CheckFeatureAccess;

public sealed record CheckFeatureAccessQuery(Guid HouseholdId, FeatureKey Feature);
