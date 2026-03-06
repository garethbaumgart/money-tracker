namespace MoneyTracker.Modules.Households.Domain;

public interface IHouseholdRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Household household, CancellationToken cancellationToken);
}
