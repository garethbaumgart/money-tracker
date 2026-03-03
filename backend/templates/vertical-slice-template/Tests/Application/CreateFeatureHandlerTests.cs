using MoneyTracker.Modules.Feature.Application.CreateFeature;
using MoneyTracker.Modules.Feature.Domain;
using Xunit;

namespace MoneyTracker.Modules.Feature.Tests.Application;

public sealed class CreateFeatureHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesFeature_WhenNameIsUnique()
    {
        var repo = new FakeFeatureRepository(exists: false);
        var handler = new CreateFeatureHandler(repo, TimeProvider.System);

        var id = await handler.HandleAsync(new CreateFeatureCommand("Demo"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.True(repo.AddWasCalled);
    }

    [Fact]
    public async Task HandleAsync_Throws_WhenAlreadyExists()
    {
        var repo = new FakeFeatureRepository(exists: true);
        var handler = new CreateFeatureHandler(repo, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new CreateFeatureCommand("Demo"), CancellationToken.None));
    }
}

internal sealed class FakeFeatureRepository(bool exists) : IFeatureRepository
{
    public bool AddWasCalled { get; private set; }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(exists);
    }

    public Task AddAsync(Feature feature, CancellationToken cancellationToken)
    {
        AddWasCalled = true;
        return Task.CompletedTask;
    }
}
