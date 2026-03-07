using MoneyTracker.Modules.Experiments.Domain;
using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Tests.Infrastructure;

public sealed class HashBasedVariantAllocatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Allocate_SameInput_ReturnsSameOutput_100Times()
    {
        var experimentId = new ExperimentId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        var firstResult = HashBasedVariantAllocator.Allocate(experimentId, userId, variants);

        for (var i = 0; i < 100; i++)
        {
            var result = HashBasedVariantAllocator.Allocate(experimentId, userId, variants);
            Assert.Equal(firstResult, result);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Allocate_FiftyFifty_DistributionWithin5Percent_Over10000Iterations()
    {
        var experimentId = new ExperimentId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var variants = new List<ExperimentVariant>
        {
            new("Control", 50),
            new("Treatment", 50)
        };

        var counts = new Dictionary<string, int> { { "Control", 0 }, { "Treatment", 0 } };

        for (var i = 0; i < 10_000; i++)
        {
            var userId = Guid.NewGuid();
            var result = HashBasedVariantAllocator.Allocate(experimentId, userId, variants);
            counts[result]++;
        }

        var controlPercentage = counts["Control"] / 10_000.0;
        var treatmentPercentage = counts["Treatment"] / 10_000.0;

        Assert.InRange(controlPercentage, 0.45, 0.55);
        Assert.InRange(treatmentPercentage, 0.45, 0.55);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Allocate_SeventyThirty_DistributionWithin5Percent_Over10000Iterations()
    {
        var experimentId = new ExperimentId(Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var variants = new List<ExperimentVariant>
        {
            new("Control", 70),
            new("Treatment", 30)
        };

        var counts = new Dictionary<string, int> { { "Control", 0 }, { "Treatment", 0 } };

        for (var i = 0; i < 10_000; i++)
        {
            var userId = Guid.NewGuid();
            var result = HashBasedVariantAllocator.Allocate(experimentId, userId, variants);
            counts[result]++;
        }

        var controlPercentage = counts["Control"] / 10_000.0;
        var treatmentPercentage = counts["Treatment"] / 10_000.0;

        Assert.InRange(controlPercentage, 0.65, 0.75);
        Assert.InRange(treatmentPercentage, 0.25, 0.35);
    }
}
