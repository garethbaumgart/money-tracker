using MoneyTracker.Modules.Experiments.Infrastructure;

namespace MoneyTracker.Modules.Experiments.Tests.Infrastructure;

public sealed class ChiSquaredSignificanceCalculatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_KnownSignificantData_ReturnsIsSignificantTrue()
    {
        // Control: 1000 total, 100 conversions (10%)
        // Treatment: 1000 total, 150 conversions (15%)
        // This is a meaningful difference with large sample size
        var result = ChiSquaredSignificanceCalculator.Calculate(
            variantATotal: 1000,
            variantAConversions: 100,
            variantBTotal: 1000,
            variantBConversions: 150);

        Assert.True(result.IsSignificant);
        Assert.True(result.PValue <= 0.05);
        Assert.False(result.SampleSizeWarning);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_NearIdenticalRates_ReturnsIsSignificantFalse()
    {
        // Control: 1000 total, 100 conversions (10%)
        // Treatment: 1000 total, 102 conversions (10.2%)
        var result = ChiSquaredSignificanceCalculator.Calculate(
            variantATotal: 1000,
            variantAConversions: 100,
            variantBTotal: 1000,
            variantBConversions: 102);

        Assert.False(result.IsSignificant);
        Assert.True(result.PValue > 0.05);
        Assert.False(result.SampleSizeWarning);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_SmallSample_ReturnsSampleSizeWarning()
    {
        // Both variants have less than 100 samples
        var result = ChiSquaredSignificanceCalculator.Calculate(
            variantATotal: 50,
            variantAConversions: 5,
            variantBTotal: 50,
            variantBConversions: 10);

        Assert.True(result.SampleSizeWarning);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_ZeroTotals_ReturnsNonSignificant()
    {
        var result = ChiSquaredSignificanceCalculator.Calculate(
            variantATotal: 0,
            variantAConversions: 0,
            variantBTotal: 0,
            variantBConversions: 0);

        Assert.False(result.IsSignificant);
        Assert.Equal(1.0, result.PValue);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Calculate_LargeSample_NoWarning()
    {
        var result = ChiSquaredSignificanceCalculator.Calculate(
            variantATotal: 500,
            variantAConversions: 50,
            variantBTotal: 500,
            variantBConversions: 75);

        Assert.False(result.SampleSizeWarning);
    }
}
