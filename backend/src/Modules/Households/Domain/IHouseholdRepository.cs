namespace MoneyTracker.Modules.Households.Domain;

public interface IHouseholdRepository
{
    Task<bool> AddIfNotExistsAsync(Household household, CancellationToken cancellationToken);
}
