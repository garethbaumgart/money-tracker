using MoneyTracker.Modules.Insights.Domain;

namespace MoneyTracker.Modules.Insights.Tests;

public class InsightsPeriodTests
{
    [Theory]
    [InlineData("7d", InsightsPeriod.SevenDays, 7)]
    [InlineData("30d", InsightsPeriod.ThirtyDays, 30)]
    [InlineData("90d", InsightsPeriod.NinetyDays, 90)]
    public void TryParse_ValidValues_ReturnsTrue(string input, InsightsPeriod expected, int expectedDays)
    {
        var success = InsightsPeriodExtensions.TryParse(input, out var period);

        Assert.True(success);
        Assert.Equal(expected, period);
        Assert.Equal(expectedDays, period.ToDays());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1d")]
    [InlineData("14d")]
    [InlineData("invalid")]
    public void TryParse_InvalidValues_ReturnsFalse(string? input)
    {
        var success = InsightsPeriodExtensions.TryParse(input, out _);

        Assert.False(success);
    }
}
