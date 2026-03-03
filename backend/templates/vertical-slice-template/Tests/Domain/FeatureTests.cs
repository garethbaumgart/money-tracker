using MoneyTracker.Modules.Feature.Domain;
using Xunit;

namespace MoneyTracker.Modules.Feature.Tests.Domain;

public sealed class FeatureTests
{
    [Fact]
    public void Create_TrimsNameAndSetsFields()
    {
        var now = DateTimeOffset.UtcNow;
        var feature = Feature.Create("  Demo  ", now);

        Assert.Equal("Demo", feature.Name);
        Assert.Equal(now, feature.CreatedAtUtc);
    }

    [Fact]
    public void Create_Throws_WhenNameMissing()
    {
        Assert.Throws<InvalidOperationException>(() => Feature.Create(" ", DateTimeOffset.UtcNow));
    }
}
