using MoneyTracker.Modules.Feature.Domain;

namespace MoneyTracker.Modules.Feature.Infrastructure;

// TODO: Replace this with your actual DB implementation.
public sealed class FeatureRepository : IFeatureRepository
{
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task AddAsync(Feature feature, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
