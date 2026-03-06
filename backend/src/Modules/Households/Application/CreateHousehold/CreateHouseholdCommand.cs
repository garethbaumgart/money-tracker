namespace MoneyTracker.Modules.Households.Application.CreateHousehold;

public sealed record CreateHouseholdCommand(string Name, Guid OwnerUserId);
