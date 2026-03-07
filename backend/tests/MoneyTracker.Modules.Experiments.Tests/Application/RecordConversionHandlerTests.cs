using MoneyTracker.Modules.Experiments.Application.RecordConversion;
using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Tests.Application;

public sealed class RecordConversionHandlerTests
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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithAllocation_RecordsConversion()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateActiveExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var userId = Guid.NewGuid();
        var allocation = ExperimentAllocation.Create(
            experiment.Id, userId, "Control", DateTimeOffset.UtcNow);
        await repo.AddAllocationAsync(allocation, CancellationToken.None);

        var handler = new RecordConversionHandler(repo);
        var result = await handler.HandleAsync(
            new RecordConversionCommand(experiment.Id, userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_DuplicateConversion_IsIdempotent()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateActiveExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var userId = Guid.NewGuid();
        var allocation = ExperimentAllocation.Create(
            experiment.Id, userId, "Control", DateTimeOffset.UtcNow);
        await repo.AddAllocationAsync(allocation, CancellationToken.None);

        var handler = new RecordConversionHandler(repo);

        var result1 = await handler.HandleAsync(
            new RecordConversionCommand(experiment.Id, userId),
            CancellationToken.None);

        var result2 = await handler.HandleAsync(
            new RecordConversionCommand(experiment.Id, userId),
            CancellationToken.None);

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithoutAllocation_ReturnsError()
    {
        var repo = new InMemoryExperimentRepository();
        var experiment = CreateActiveExperiment();
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        var handler = new RecordConversionHandler(repo);
        var result = await handler.HandleAsync(
            new RecordConversionCommand(experiment.Id, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExperimentErrors.AllocationNotFound, result.ErrorCode);
    }
}
