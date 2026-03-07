namespace MoneyTracker.Modules.Experiments.Infrastructure;

public static class ChiSquaredSignificanceCalculator
{
    public static SignificanceResult Calculate(
        int variantATotal,
        int variantAConversions,
        int variantBTotal,
        int variantBConversions)
    {
        var sampleSizeWarning = variantATotal < 100 || variantBTotal < 100;

        var variantANonConversions = variantATotal - variantAConversions;
        var variantBNonConversions = variantBTotal - variantBConversions;

        var totalConversions = variantAConversions + variantBConversions;
        var totalNonConversions = variantANonConversions + variantBNonConversions;
        var grandTotal = variantATotal + variantBTotal;

        if (grandTotal == 0 || totalConversions == 0 || totalNonConversions == 0)
        {
            return new SignificanceResult(
                ChiSquaredStatistic: 0,
                PValue: 1.0,
                IsSignificant: false,
                SampleSizeWarning: sampleSizeWarning);
        }

        // Expected values for 2x2 contingency table
        var expectedAConversions = (double)variantATotal * totalConversions / grandTotal;
        var expectedANonConversions = (double)variantATotal * totalNonConversions / grandTotal;
        var expectedBConversions = (double)variantBTotal * totalConversions / grandTotal;
        var expectedBNonConversions = (double)variantBTotal * totalNonConversions / grandTotal;

        // Chi-squared statistic: sum of (O-E)^2/E
        var chiSquared = 0.0;
        chiSquared += Square(variantAConversions - expectedAConversions) / expectedAConversions;
        chiSquared += Square(variantANonConversions - expectedANonConversions) / expectedANonConversions;
        chiSquared += Square(variantBConversions - expectedBConversions) / expectedBConversions;
        chiSquared += Square(variantBNonConversions - expectedBNonConversions) / expectedBNonConversions;

        // Approximate p-value using critical values for df=1
        var pValue = ApproximatePValue(chiSquared);
        var isSignificant = pValue < 0.05;

        return new SignificanceResult(
            ChiSquaredStatistic: chiSquared,
            PValue: pValue,
            IsSignificant: isSignificant,
            SampleSizeWarning: sampleSizeWarning);
    }

    private static double Square(double x) => x * x;

    private static double ApproximatePValue(double chiSquared)
    {
        // Critical values for chi-squared distribution with df=1:
        // 3.841 → p < 0.05
        // 6.635 → p < 0.01
        // 10.828 → p < 0.001
        if (chiSquared >= 10.828)
        {
            return 0.001;
        }

        if (chiSquared >= 6.635)
        {
            return 0.01;
        }

        if (chiSquared >= 3.841)
        {
            return 0.05;
        }

        // For values below significance threshold, return a rough approximation
        // Linear interpolation from 0 to 3.841 mapping to 1.0 to 0.05
        if (chiSquared <= 0)
        {
            return 1.0;
        }

        return 1.0 - (chiSquared / 3.841) * 0.95;
    }
}

public sealed record SignificanceResult(
    double ChiSquaredStatistic,
    double PValue,
    bool IsSignificant,
    bool SampleSizeWarning);
