namespace MoneyTracker.Modules.Households.Application.GetHouseholdDashboard;

public sealed record GetHouseholdDashboardQuery(Guid HouseholdId, Guid RequestingUserId);
