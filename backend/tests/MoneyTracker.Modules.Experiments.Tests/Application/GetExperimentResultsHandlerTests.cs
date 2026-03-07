using MoneyTracker.Modules.Experiments.Application.GetExperimentResults;
using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Tests.Application;

public sealed class GetExperimentResultsHandlerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_ReturnsCorrectConversionRates()
    {
        var repo = new InMemoryExperimentRepository();

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
        await repo.AddExperimentAsync(experiment, CancellationToken.None);

        // Add 10 allocations to Control, 5 conversions
        for (var i = 0; i < 10; i++)
        {
            var userId = Guid.NewGuid();
            var allocation = ExperimentAllocation.Create(
                experiment.Id, userId, "Control", DateTimeOffset.UtcNow);
            await repo.AddAllocationAsync(allocation, CancellationToken.None);

            if (i < 5)
            {
                var conversion = ConversionEvent.Create(
                    experiment.Id, userId, "Control", DateTimeOffset.UtcNow);
                await repo.AddConversionEventAsync(conversion, CancellationToken.None);
            }
        }

        // Add 10 allocations to Treatment, 8 conversions
        for (var i = 0; i < 10; i++)
        {
            var userId = Guid.NewGuid();
            var allocation = ExperimentAllocation.Create(
                experiment.Id, userId, "Treatment", DateTimeOffset.UtcNow);
            await repo.AddAllocationAsync(allocation, CancellationToken.None);

            if (i < 8)
            {
                var conversion = ConversionEvent.Create(
                    experiment.Id, userId, "Treatment", DateTimeOffset.UtcNow);
                await repo.AddConversionEventAsync(conversion, CancellationToken.None);
            }
        }

        var handler = new GetExperimentResultsHandler(repo);
        var result = await handler.HandleAsync(
            new GetExperimentResultsQuery(experiment.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Variants!.Count);

        var controlVariant = result.Variants.First(v => v.VariantName == "Control");
        Assert.Equal(10, controlVariant.TotalAllocations);
        Assert.Equal(5, controlVariant.Conversions);
        Assert.Equal(0.5, controlVariant.ConversionRate);

        var treatmentVariant = result.Variants.First(v => v.VariantName == "Treatment");
        Assert.Equal(10, treatmentVariant.TotalAllocations);
        Assert.Equal(8, treatmentVariant.Conversions);
        Assert.Equal(0.8, treatmentVariant.ConversionRate);

        // With small sample sizes, expect sample size warning
        Assert.True(result.SampleSizeWarning!.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WithNonExistentExperiment_ReturnsError()
    {
        var repo = new InMemoryExperimentRepository();
        var handler = new GetExperimentResultsHandler(repo);

        var result = await handler.HandleAsync(
            new GetExperimentResultsQuery(ExperimentId.New()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExperimentErrors.ExperimentNotFound, result.ErrorCode);
    }
}
