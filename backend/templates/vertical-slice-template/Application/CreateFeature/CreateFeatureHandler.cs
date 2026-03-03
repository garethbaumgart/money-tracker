using MoneyTracker.Modules.Feature.Domain;

namespace MoneyTracker.Modules.Feature.Application.CreateFeature;

public sealed class CreateFeatureHandler(IFeatureRepository repository, TimeProvider timeProvider)
{
    public async Task<Guid> HandleAsync(CreateFeatureCommand command, CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsByNameAsync(command.Name, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(FeatureErrors.AlreadyExists);
        }

        var feature = Feature.Create(command.Name, timeProvider.GetUtcNow());
        await repository.AddAsync(feature, cancellationToken);

        return feature.Id.Value;
    }
}
