using MoneyTracker.Modules.Experiments.Application.AllocateUser;
using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Tests.Application;

public sealed class AllocateUserHandlerTests
{
    private static Experiment CreateActiveExperiment()
    {
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        var experiment = Experiment.Create(
            "Test Experiment",
            "Description",
            variants,
            "conversion_rate",
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"));

        experiment.Activate();
        return experiment;
    }

    private static Experiment CreateDraftExperiment()
    {
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        return Experiment.Create(
            "Draft Experiment",
            "Description",
            variants,
            "conversion_rate",
            DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-04-01T00:00:00Z"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithDraftExperiment_ReturnsError()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateDraftExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var handler = new AllocateUserHandler(repo);
        var result = await handler.HandleAsync(
            new AllocateUserCommand(experiment.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExperimentErrors.ExperimentNotActive, result.ErrorCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithActiveExperiment_ReturnsAllocation()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateActiveExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var handler = new AllocateUserHandler(repo);
        var userId = Guid.NewGuid();
        var result = await handler.HandleAsync(
            new AllocateUserCommand(experiment.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(experiment.Id, result.ExperimentId);
        Assert.Equal("Test Experiment", result.ExperimentName);
        Assert.NotNull(result.VariantName);
        Assert.NotNull(result.AllocatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_AllocateTwice_ReturnsSameVariant_Sticky()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateActiveExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var handler = new AllocateUserHandler(repo);
        var userId = Guid.NewGuid();

        var result1 = await handler.HandleAsync(
            new AllocateUserCommand(experiment.Id, userId),
            CancellationToken.None);

        var result2 = await handler.HandleAsync(
            new AllocateUserCommand(experiment.Id, userId),
            CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal(result1.VariantName, result2.VariantName);
        Assert.Equal(result1.AllocatedAtUtc, result2.AllocatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithNonExistentExperiment_ReturnsError()
    {
        var repo = new InMemoryExperimentRepository();
        var handler = new AllocateUserHandler(repo);

        var result = await handler.HandleAsync(
            new AllocateUserCommand(ExperimentId.New(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExperimentErrors.ExperimentNotFound, result.ErrorCode);
    }
}
