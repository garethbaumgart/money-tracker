namespace MoneyTracker.Modules.Households.Application.GetCurrentBudgetSnapshot;

public sealed record GetCurrentBudgetSnapshotQuery(Guid HouseholdId, Guid RequestingUserId);
