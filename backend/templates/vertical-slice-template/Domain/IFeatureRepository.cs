namespace MoneyTracker.Modules.Feature.Domain;

public interface IFeatureRepository
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(Feature feature, CancellationToken cancellationToken);
}
